from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "MainWindow.xaml.cs"
HELPER = ROOT / "MainWindow.RefactoredMethods.cs"
MARKER = "// AUTO-REFACTORED-METHODS-V1"

TARGETS = [
    "OpenChartTabAsync",
    "CreateTabHeader",
    "SetBusy",
    "DeleteSymbolsButton_Click",
    "MakeWatchButton_Click",
    "EnterFullScreen",
    "ExitFullScreen",
    "ApplySymbolFiltersThroughEngineAsync",
    "AttachSymbolFilterArchitecture",
]

# The first item has two overloads. Move only the overload that accepts
# replaceCurrentTab; the two-argument public wrapper remains in MainWindow.

def method_start_candidates(text, name):
    pat = re.compile(
        rf"(?ms)^[ \t]*(?:public|private|protected|internal)\b.*?\b{name}\s*\([^;{{}}]*?\)\s*\{{"
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
    candidates = method_start_candidates(text, name)
    for m in candidates:
        open_index = text.find("{", m.start(), m.end())
        close_index = matching_brace(text, open_index)
        block = text[m.start():close_index + 1]
        if predicate is None or predicate(block):
            return m.start(), close_index + 1, block
    raise RuntimeError(f"Target method not found: {name}")


def main():
    if not SOURCE.exists():
        raise RuntimeError("MainWindow.xaml.cs not found")
    if HELPER.exists() and MARKER in HELPER.read_text(encoding="utf-8"):
        print("Refactor already applied; nothing to do.")
        return

    source = SOURCE.read_text(encoding="utf-8")
    original = source
    extracted = []

    # Extract from bottom to top so offsets remain valid.
    specs = [
        ("OpenChartTabAsync", lambda b: "bool replaceCurrentTab" in b),
        ("CreateTabHeader", None),
        ("SetBusy", lambda b: "SetBusy(" in b),
        ("DeleteSymbolsButton_Click", None),
        ("MakeWatchButton_Click", None),
        ("EnterFullScreen", None),
        ("ExitFullScreen", None),
        ("ApplySymbolFiltersThroughEngineAsync", None),
        ("AttachSymbolFilterArchitecture", None),
    ]

    ranges = []
    for name, predicate in specs:
        start, end, block = extract_one(source, name, predicate)
        ranges.append((start, end, name, block))

    # Reject overlapping selections before changing anything.
    ordered = sorted(ranges)
    for (_, prev_end, _, _), (start, _, _, _) in zip(ordered, ordered[1:]):
        if start < prev_end:
            raise RuntimeError("Overlapping target method ranges detected")

    for start, end, name, block in sorted(ranges, reverse=True):
        extracted.append((name, block))
        source = source[:start] + source[end:]

    expected = len(specs)
    if len(extracted) != expected:
        raise RuntimeError(f"Expected {expected} methods, extracted {len(extracted)}")

    # Preserve the original usings so moved handlers compile independently.
    using_end = source.find("namespace TradeIt")
    if using_end < 0:
        raise RuntimeError("namespace TradeIt not found")
    usings = source[:using_end].rstrip()

    helper = (
        usings
        + "\n\n"
        + "namespace TradeIt\n{\n"
        + "    public partial class MainWindow\n    {\n"
        + f"        {MARKER}\n\n"
        + "\n\n".join(block for _, block in reversed(extracted))
        + "\n    }\n}\n"
    )

    # Keep a marker in the source as well, making the operation visibly one-time.
    source = source.replace(
        "public partial class MainWindow\n    {",
        "public partial class MainWindow\n    {\n        // AUTO-REFACTORED-METHODS-V1: implementations moved to MainWindow.RefactoredMethods.cs",
        1,
    )

    SOURCE.write_text(source, encoding="utf-8", newline="\n")
    HELPER.write_text(helper, encoding="utf-8", newline="\n")

    subprocess.run(["git", "diff", "--check"], cwd=ROOT, check=True)
    print("Extracted methods:")
    for name, _ in reversed(extracted):
        print(f"  - {name}")


if __name__ == "__main__":
    main()
