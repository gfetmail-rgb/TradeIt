from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[1]
MARKER = "// AUTO-REFACTORED-METHODS-V2"
HELPER = ROOT / "MainWindow.RefactoredMethods.cs"

# Each tuple is (source file, method name, optional predicate for overload selection).
TARGETS = [
    ("MainWindow.xaml.cs", "OpenChartTabAsync", lambda b: "bool replaceCurrentTab" in b),
    ("MainWindow.xaml.cs", "CreateTabHeader", None),
    ("MainWindow.xaml.cs", "SetBusy", None),
    ("MainWindow.xaml.cs", "DeleteSymbolsButton_Click", None),
    ("MainWindow.xaml.cs", "MakeWatchButton_Click", None),
    ("MainWindow.xaml.cs", "EnterFullScreen", None),
    ("MainWindow.xaml.cs", "ExitFullScreen", None),
    ("MainWindow.SymbolFilterArchitecture.cs", "ApplySymbolFiltersThroughEngineAsync", None),
    ("MainWindow.SymbolFilterArchitecture.cs", "AttachSymbolFilterArchitecture", None),
]


def method_start_candidates(text, name):
    # Handles multiline signatures while requiring an access modifier.
    pat = re.compile(
        rf"(?ms)^[ \t]*(?:public|private|protected|internal)\b[^;{{}}]*?\b{name}\s*\([^;{{}}]*?\)\s*\{{"
    )
    return list(pat.finditer(text))


def matching_brace(text, open_index):
    depth = 0
    i = open_index
    state = "code"
    while i < len(text):
        c = text[i]
        n = text[i + 1] if i + 1 < len(text) else ""
        if state == "code":
            if c == '"':
                state = "string"
            elif c == "'":
                state = "char"
            elif c == "/" and n == "/":
                state = "line_comment"
                i += 1
            elif c == "/" and n == "*":
                state = "block_comment"
                i += 1
            elif c == "{":
                depth += 1
            elif c == "}":
                depth -= 1
                if depth == 0:
                    return i
        elif state == "string":
            if c == "\\":
                i += 1
            elif c == '"':
                state = "code"
        elif state == "char":
            if c == "\\":
                i += 1
            elif c == "'":
                state = "code"
        elif state == "line_comment":
            if c == "\n":
                state = "code"
        elif state == "block_comment":
            if c == "*" and n == "/":
                state = "code"
                i += 1
        i += 1
    raise RuntimeError("Unbalanced braces")


def extract_one(text, name, predicate=None):
    for m in method_start_candidates(text, name):
        open_index = text.find("{", m.start(), m.end())
        close_index = matching_brace(text, open_index)
        block = text[m.start():close_index + 1]
        if predicate is None or predicate(block):
            return m.start(), close_index + 1, block
    raise RuntimeError(f"Target method not found: {name}")


def extract_from_file(path, specs):
    text = path.read_text(encoding="utf-8")
    ranges = []
    for name, predicate in specs:
        start, end, block = extract_one(text, name, predicate)
        ranges.append((start, end, name, block))
    ordered = sorted(ranges)
    for (_, prev_end, _, _), (start, _, _, _) in zip(ordered, ordered[1:]):
        if start < prev_end:
            raise RuntimeError(f"Overlapping target ranges in {path.name}")
    for start, end, _, _ in sorted(ranges, reverse=True):
        text = text[:start] + text[end:]
    return text, [(name, block) for _, _, name, block in ranges]


def main():
    if HELPER.exists() and MARKER in HELPER.read_text(encoding="utf-8"):
        print("Refactor already applied; nothing to do.")
        return

    grouped = {}
    for filename, name, predicate in TARGETS:
        grouped.setdefault(filename, []).append((name, predicate))

    extracted_all = []
    updated_files = {}
    for filename, specs in grouped.items():
        path = ROOT / filename
        if not path.exists():
            raise RuntimeError(f"Missing source file: {filename}")
        updated, extracted = extract_from_file(path, specs)
        updated_files[path] = updated
        extracted_all.extend(extracted)

    if len(extracted_all) != len(TARGETS):
        raise RuntimeError("Not all requested methods were extracted")

    # Preserve the union of using directives from the affected files.
    using_blocks = []
    for filename in grouped:
        text = (ROOT / filename).read_text(encoding="utf-8")
        namespace_pos = text.find("namespace TradeIt")
        if namespace_pos < 0:
            raise RuntimeError(f"namespace TradeIt not found in {filename}")
        using_blocks.append(text[:namespace_pos].rstrip())
    usings = "\n".join(dict.fromkeys(using_blocks))

    helper = (
        usings
        + "\n\nnamespace TradeIt\n{\n"
        + "    public partial class MainWindow\n    {\n"
        + f"        {MARKER}\n\n"
        + "\n\n".join(block for _, block in extracted_all)
        + "\n    }\n}\n"
    )

    for path, content in updated_files.items():
        if "AUTO-REFACTORED-METHODS-V2" not in content:
            content = content.replace(
                "public partial class MainWindow\n    {",
                "public partial class MainWindow\n    {\n        // AUTO-REFACTORED-METHODS-V2: implementations moved to MainWindow.RefactoredMethods.cs",
                1,
            )
        path.write_text(content, encoding="utf-8", newline="\n")

    HELPER.write_text(helper, encoding="utf-8", newline="\n")
    subprocess.run(["git", "diff", "--check"], cwd=ROOT, check=True)
    print("Extracted methods:")
    for name, _ in extracted_all:
        print(f"  - {name}")


if __name__ == "__main__":
    main()
