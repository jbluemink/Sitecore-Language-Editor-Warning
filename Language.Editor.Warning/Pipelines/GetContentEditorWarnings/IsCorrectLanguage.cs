using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Sitecore.Collections;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Globalization;
using Sitecore.Pipelines.GetContentEditorWarnings;
using Version = Sitecore.Data.Version;



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
            var nohit = true;
            foreach (var site in global::Sitecore.Configuration.Settings.Sites)
            {
                if (path.StartsWith(site.RootPath) && site.Name != "shell" && site.Name != "modules_shell" &&
                    site.Name != "modules_website" && site.RootPath.Trim() != string.Empty)
                {
                    nohit = false;
                    var language = site.Language;
                    if (string.IsNullOrEmpty(language))
                    {
                        //language attribuut is optioneel, is die er niet gebruik dan de default language.
                        language = Sitecore.Configuration.Settings.DefaultLanguage;
                    }
                    string altLanguages = site.Properties.Get("altLanguage");
                    if (!string.IsNullOrEmpty(altLanguages))
                    {
                        altLanguages = "," + altLanguages.Trim().Replace(" ", "").Replace("|", ",") + ",";
                    }

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
                    var fallbackfound = string.Empty;
                    foreach (var lan in languageList)
                    {
                        if (lan.Trim() != string.Empty)
                        {
                            var lanItem = GetLanguageVersion(item, lan);
                            if (lanItem == null)
                            {
                                if (versionnotfound != string.Empty)
                                {
                                    versionnotfound += ",";
                                }
                                versionnotfound += lan;
                            }
                            else if (lanItem.Language != lanItem.OriginalLanguage)
                            {
                                if (fallbackfound != string.Empty)
                                {
                                    fallbackfound += ",";
                                }
                                fallbackfound += lan + "#" + lanItem.OriginalLanguage.Name;
                            }
                        }
                    }
                    if (versionnotfound != string.Empty || fallbackfound != string.Empty)
                    {
                        AddTranslateWarning(item, args, versionnotfound, fallbackfound, site.Name);
                    }
                }
            }
            if (nohit)
            {
                //item is not in a website, so a system/template/layout item. maby it is nice to see the "en" version
                ProcessNonSiteItem(item,args);
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

        private static void AddTranslateWarning(Item item, GetContentEditorWarningsArgs args, string language, string fallback, string sitename)
        {
            GetContentEditorWarningsArgs.ContentEditorWarning warning = args.Add();
            warning.Title = "This item is not translated for the site: " + sitename;
            warning.Text = "Switch to the not translated language and create a version";
            if (language != string.Empty)
            {
                var languageList = language.Split(',');

                foreach (var languageitem in languageList)
                {
                    warning.AddOption(string.Format("Switch to {0}", languageitem), string.Format(CultureInfo.InvariantCulture, "item:load(id={0},language={1})", item.ID, languageitem));
                }
            }
            if (fallback != string.Empty)
            {
                var languageList = fallback.Split(',');

                foreach (var languageitem in languageList)
                {
                    string[] languageset = languageitem.Split('#');
                    if (languageset.Length > 1)
                    {
                        warning.AddOption(string.Format("Switch to {0} (now uses {1} language fallback)", languageset[0], languageset[1]), string.Format(CultureInfo.InvariantCulture, "item:load(id={0},language={1})", item.ID, languageset[0]));
                    }
                }
            }
            warning.IsExclusive = false;

        }

        private static void ProcessNonSiteItem(Item item, GetContentEditorWarningsArgs args)
        {

            Version[] versionNumbers = item.Versions.GetVersionNumbers(false);
            if (versionNumbers != null && versionNumbers.Length > 0)
                return;

            LanguageCollection languages = LanguageManager.GetLanguages(Sitecore.Context.ContentDatabase);
            int lancount = 0;
            var languageList = new List<string>();
            foreach (Sitecore.Globalization.Language language in languages)
            {
                if (HasLanguage(item, language))
                {
                    lancount++;
                    languageList.Add(language.ToString());
                    if (lancount > 3)
                    {
                        //limiet to 4, but add en
                        if (!languageList.Contains("en"))
                        {
                            var defaultlang = Sitecore.Globalization.Language.Parse("en");
                            if (defaultlang != null && HasLanguage(item, defaultlang))
                            {
                                languageList.Add(defaultlang.ToString());
                            }
                        }
                        break;
                    }
                }
            }
            if (languageList.Any())
            {
                GetContentEditorWarningsArgs.ContentEditorWarning contentEditorWarning = args.Add();
                contentEditorWarning.Title =
                    string.Format(Translate.Text("The current item does not have a version in \"{0}\"."),
                        (object)item.Language.GetDisplayName());
                if (item.Access.CanWriteLanguage() && item.Access.CanWrite())
                {
                    contentEditorWarning.Text =
                        Translate.Text("To create a version, click Add a New Version or Switch language.");
                    contentEditorWarning.AddOption(Translate.Text("Add a new version."), "item:addversion");
                    foreach (var languageitem in languageList)
                    {
                        contentEditorWarning.AddOption(string.Format("Switch to {0}", languageitem), string.Format(CultureInfo.InvariantCulture, "item:load(id={0},language={1})", item.ID, languageitem));
                    }
                    contentEditorWarning.IsExclusive = true;
                }
                else
                    contentEditorWarning.IsExclusive = false;
                contentEditorWarning.HideFields = true;
                contentEditorWarning.Key = HasNoVersions.Key;
            }
            else
            {
                GetContentEditorWarningsArgs.ContentEditorWarning contentEditorWarning = args.Add();
                contentEditorWarning.Title =
                    string.Format(Translate.Text("The current item does not have a version in \"{0}\"."),
                        (object) item.Language.GetDisplayName());
                if (item.Access.CanWriteLanguage() && item.Access.CanWrite())
                {
                    contentEditorWarning.Text =
                        Translate.Text("To create a version, click Add a New Version or click Add on the Versions tab.");
                    contentEditorWarning.AddOption(Translate.Text("Add a new version."), "item:addversion");
                    contentEditorWarning.IsExclusive = true;
                }
                else
                    contentEditorWarning.IsExclusive = false;
                contentEditorWarning.HideFields = true;
                contentEditorWarning.Key = HasNoVersions.Key;
            }
        }

        public static bool HasLanguage(Item item, Sitecore.Globalization.Language language)
        {
            return ItemManager.GetVersions(item, language).Count > 0;
        }
    }
}