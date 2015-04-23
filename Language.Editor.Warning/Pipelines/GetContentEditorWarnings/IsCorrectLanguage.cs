using System.Globalization;
using Sitecore.Data.Items;
using Sitecore.Pipelines.GetContentEditorWarnings;

namespace Language.Editor.Warning.Pipelines.GetContentEditorWarnings
{
    /*

    Sitecore Language editor Warning.
    Add the processor to the pipeline with the following file in the app_config/include
     
     <configuration xmlns:patch="http://www.sitecore.net/xmlconfig/">
      <sitecore>
        <pipelines>
          <getContentEditorWarnings>
            <processor type="Language.Editor.Warning.Pipelines.GetContentEditorWarnings.IsCorrectLanguage, Language.Editor.Warning" patch:before="processor[@type='Sitecore.Pipelines.GetContentEditorWarnings.ItemNotFound, Sitecore.Kernel']"/>
          </getContentEditorWarnings>
        </pipelines>
      </sitecore>
    </configuration>
     
    The processor use the <sites><site> nodes. and read the language and optional the custom altLanguage propertie.
    
    Have fun,
    Jan Bluemink, jan@mirabeau.nl
    */

    /// <summary>
    /// Geef een Content editor waarschuwing bij de Verkeerde taal.
    /// </summary>
    class IsCorrectLanguage
    {
 
        public void Process(GetContentEditorWarningsArgs args)
        {
            Item item = args.Item;
            if (item == null)
            {
                return;
            }
 
            GetWebsite(item, args);
        }

        public static Item GetLanguageVersion(Item item, string languageName)
        {
            var language = global::Sitecore.Globalization.Language.Parse(languageName);
            if (language != null)
            {
                var languageSpecificItem = item.Database.GetItem(item.ID, language);
                if (languageSpecificItem != null && languageSpecificItem.Versions.Count > 0)
                {
                    return languageSpecificItem;
                }
            }
            return null;
        }

        private static void GetWebsite(Item item, GetContentEditorWarningsArgs args)
        {
            var path = item.Paths.FullPath;
            var itemlanguage = item.Language.ToString();
            foreach (var site in global::Sitecore.Configuration.Settings.Sites)
            {
                if (path.StartsWith(site.RootPath) && site.Name != "shell" && site.Name != "modules_shell" &&
                    site.Name != "modules_website" && site.RootPath.Trim() != string.Empty)
                {
                    var language = site.Language;
                    if (string.IsNullOrEmpty(language))
                    {
                        //language attribuut is optioneel, is die er niet gebruik dan de default language.
                        language = Sitecore.Configuration.Settings.DefaultLanguage;
                    }
                    string altLanguages = site.Properties.Get("altLanguage");
                    altLanguages = "," + altLanguages.Trim().Replace(" ", "").Replace("|", ",") + ",";
                    //altLanguage is optioneel en mag comma of | seperated zijn.
                    if (System.String.Compare(itemlanguage, language, System.StringComparison.OrdinalIgnoreCase) != 0)
                    {
                        if (string.IsNullOrEmpty(altLanguages))
                        {
                            AddWarning(item, args, language, site.Name);
                            return;
                        }
                        else
                        {
                            if (!altLanguages.Contains("," + itemlanguage + ","))
                            {
                                AddWarning(item, args, language + altLanguages, site.Name);
                                return;
                            }
                        }
                    }

                    var languageList = (language + altLanguages).Split(',');
                    var versionnotfound = string.Empty;
                    foreach (var lan in languageList)
                    {
                        if (lan.Trim() != string.Empty)
                        {
                            if (GetLanguageVersion(item, lan) == null)
                            {
                                if (versionnotfound != string.Empty)
                                {
                                    versionnotfound += ",";
                                }
                                versionnotfound += lan;
                            }
                        }
                    }
                    if (versionnotfound != string.Empty)
                    {
                        AddTranslateWarning(item, args, versionnotfound, site.Name);
                    }
                }
            }
        }
    
        public static void AddWarning(Item item, GetContentEditorWarningsArgs args, string language, string sitename)
        {
            GetContentEditorWarningsArgs.ContentEditorWarning warning = args.Add();

            warning.Title = "You are not in the default language of the current site: " + sitename;
            warning.Text = "Switch to the correct language";
            var languageList = language.Split(',');
            foreach (var languageitem in languageList)
            {
                if (!string.IsNullOrWhiteSpace(languageitem))
                {
                    warning.AddOption(string.Format("Switch to {0}", languageitem),
                        string.Format(CultureInfo.InvariantCulture, "item:load(id={0},language={1})", item.ID,
                            languageitem));
                }
            }
            warning.IsExclusive = true;
        }

        private static void AddTranslateWarning(Item item, GetContentEditorWarningsArgs args, string language, string sitename)
        {
            GetContentEditorWarningsArgs.ContentEditorWarning warning = args.Add();
            warning.Title = "This item is not translated for the site: " + sitename;
            warning.Text = "Switch to the not translated language and create a version";
            var languageList = language.Split(',');

            foreach (var languageitem in languageList)
            {
                warning.AddOption(string.Format("Switch to {0}", languageitem), string.Format(CultureInfo.InvariantCulture, "item:load(id={0},language={1})", item.ID, languageitem));
            }

            warning.IsExclusive = false;

        }
    }
}