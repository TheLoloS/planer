namespace planer
{
    /// <summary>
    /// Simple runtime localization helper. Holds currently selected language code ("en" or "pl")
    /// and provides a Get(key) method returning localized strings. This is a minimal approach
    /// for the small application and avoids resource files for simplicity.
    /// </summary>
    public static class Localizer
    {
        private static string _language = "en";

        public static string Language
        {
            get => _language;
            set => _language = (value ?? "en").ToLowerInvariant();
        }

        private static readonly System.Collections.Generic.Dictionary<string, string> en = new()
        {
            {"OpenMenu","Open Schedule"},
            {"EditConfig","Edit configuration"},
            {"ReloadConfig","Reload configuration"},
            {"Exit","Exit"},
            {"TrayLoading","Day Scheduler (Loading...)"},
            {"StatusPrefix","Status:"},
            {"RemainingPrefix","Remaining:"},
            {"WorkLabel","work"},
            {"BreakLabel","break"},
            {"MissingSoundFileTitle","Missing sound file"},
            {"ConfigurationReloaded","Configuration reloaded"}
        };

        private static readonly System.Collections.Generic.Dictionary<string, string> pl = new()
        {
            {"OpenMenu","Otwórz Harmonogram"},
            {"EditConfig","Edytuj konfiguracjê"},
            {"ReloadConfig","Prze³aduj konfiguracjê"},
            {"Exit","Zamknij Harmonogram"},
            {"TrayLoading","Harmonogram Dnia (£adowanie...)"},
            {"StatusPrefix","Status:"},
            {"RemainingPrefix","Pozosta³o:"},
            {"WorkLabel","praca"},
            {"BreakLabel","przerwa"},
            {"MissingSoundFileTitle","Brak pliku dŸwiêkowego"},
            {"ConfigurationReloaded","Prze³adowano konfiguracjê"}
        };

        public static string Get(string key)
        {
            var dict = _language == "pl" ? pl : en;
            if (dict.TryGetValue(key, out var v)) return v;
            // fallback: attempt other language then return key as last resort
            var other = _language == "pl" ? en : pl;
            if (other.TryGetValue(key, out v)) return v;
            return key;
        }
    }
}
