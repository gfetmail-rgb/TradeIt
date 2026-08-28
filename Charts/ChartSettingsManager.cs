namespace TradeIt.Charts
{
    public static class ChartSettingsManager
    {
        // =========================================================
        // آخرین تنظیمات ذخیره‌شده
        //
        // فقط Chart های جدید از این تنظیمات استفاده می‌کنند.
        // Chart های باز قبلی هیچ تغییری نمی‌کنند.
        // =========================================================

        private static ChartSettings _current =
            new ChartSettings();


        // =========================================================
        // آخرین تنظیمات
        // =========================================================

        public static ChartSettings Current
        {
            get
            {
                return _current.Clone();
            }
        }


        // =========================================================
        // ثبت تنظیمات جدید
        // =========================================================

        public static void SetDefaults(
            ChartSettings settings)
        {
            if (settings == null)
                return;

            _current =
                settings.Clone();
        }


        // =========================================================
        // Clone
        //
        // برای سازگاری با کدهای فعلی پروژه
        // =========================================================

        public static ChartSettings Clone(
            ChartSettings settings)
        {
            if (settings == null)
                return new ChartSettings();

            return settings.Clone();
        }
    }
}