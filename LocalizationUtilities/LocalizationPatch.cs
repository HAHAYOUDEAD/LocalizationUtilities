using HarmonyLib;
using Il2Cpp;
using System.Text.RegularExpressions;
using StringTableEntry = Il2Cpp.StringTableData.Entry;

namespace LocalizationUtilities;

[HarmonyPatch]
internal static class LocalizationPatch
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(Localization), nameof(Localization.LoadStringTableForLanguage))]
	private static void AddCustomLocalizationsToStringTable()
	{
		StringTable strTable = Localization.s_CurrentLanguageStringTable;
		foreach (LocalizationSet set in LocalizationManager.Localizations)
		{
			AddOrUpdate(strTable, set);
		}
	}

    private static void AddOrUpdate(StringTable stringTable, LocalizationSet set)
    {
        StringTable fallbackTable = Localization.s_FallbackStringTable;
        bool fallbackExists = fallbackTable != stringTable; // current language is not English
        string[] languages = stringTable.GetLanguagesArray();
        foreach (LocalizationEntry entry in set.Entries)
        {
            StringTableEntry stringEntry = stringTable.GetOrAddEntryFromKey(entry.LocalizationID);

            if (fallbackExists)
            {
                StringTableEntry fallbackEntry = fallbackTable.GetOrAddEntryFromKey(entry.LocalizationID);

                if (set.DefaultToEnglish && entry.Map.TryGetValue("English", out string? text2))
                {
                    if (fallbackEntry.m_Languages.Count > 0) fallbackEntry.m_Languages[0] = text2; // 0 is always English in fallback table
                }
            }

            for (int i = 0; i < languages.Length; i++)
            {
                string language = languages[i];

                string pattern = @"\[(.*?)\]";
                Match match = Regex.Match(language, pattern);
                if (match.Success)
                {
                    language = match.Groups[1].Value;
                }

                if (entry.Map.TryGetValue(language, out string? text) && !string.IsNullOrWhiteSpace(text))
                {
                    stringEntry.m_Languages[i] = text;
                }
            }
        }
    }

    private static string[] GetLanguagesArray(this StringTable stringTable)
	{
		return stringTable.GetLanguages().ToArray().ToArray();
	}

	private static StringTableEntry GetOrAddEntryFromKey(this StringTable stringTable, string localizationID)
	{
		return stringTable.GetEntryFromKey(localizationID) ?? stringTable.AddEntryForKey(localizationID);
	}
}
