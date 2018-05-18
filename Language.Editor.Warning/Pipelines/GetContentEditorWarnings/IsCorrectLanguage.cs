using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Sitecore.Collections;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Globalization;
using Sitecore.Pipelines.GetContentEditorWarnings;
using Sitecore.Web;
using Version = Sitecore.Data.Version;

namespace Stockpick.Language.Editor.Warning.Pipelines.GetContentEditorWarnings
{
    /*

    Sitecore Language editor Warning.
    Add the processor to the pipeline with the following file in the app_config/include
     
     <configuration xmlns:patch="http://www.sitecore.net/xmlconfig/">
      <sitecore>
        <pipelines>
          <getContentEditorWarnings>
            <processor type="Stockpick.Language.Editor.Warning.Pipelines.GetContentEditorWarnings.IsCorrectLanguage, Stockpick.Language.Editor.Warning" patch:before="processor[@type='Sitecore.Pipelines.GetContentEditorWarnings.ItemNotFound, Sitecore.Kernel']"/>
          </getContentEditorWarnings>
        </pipelines>
      </sitecore>
    </configuration>
     
    The processor use the <sites><site> nodes. and read the language and optional the custom altLanguage propertie.
    
    based on https://github.com/jbluemink/Sitecore-Language-Editor-Warning
    */

    /// <summary>
    /// Give a Content editor warning if editing in the wrong language
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

            Check(item, args);
        }

        public static Item GetLanguageVersion(Item item, string languageName)
        {
            var language = Sitecore.Globalization.Language.Parse(languageName);
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

        private static SiteInfo GetSiteInfo(Item item)
        {
            return Sitecore.Sites.SiteContextFactory.Sites
                .Where(s => !string.IsNullOrWhiteSpace(s.RootPath) && item.Paths.Path.StartsWith(s.RootPath, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(s => s.RootPath.Length)
                .FirstOrDefault();
            // for this works beter than Sitecore.Links.LinkManager.ResolveTargetSite
        }

        private static void Check(Item item, GetContentEditorWarningsArgs args)
        {
            var path = item.Paths.FullPath;
            if (path.StartsWith("/sitecore/content/"))
            {
                var itemlanguage = item.Language.ToString();
                var site = GetSiteInfo(item);

                var defaultLanguage = site.Language;
                if (string.IsNullOrEmpty(defaultLanguage))
                {
                    //language attribuut is optioneel, if not present use default language.
                    defaultLanguage = Sitecore.Configuration.Settings.DefaultLanguage;
                }
                var altLanguages = GetAltLanguagesFromConfig(site);
                if (string.IsNullOrEmpty(altLanguages))
                {
                    altLanguages = GetAltLanguagesFromRootItem(site, item);
                    if (!string.IsNullOrEmpty(altLanguages))
                    {
                        //language configuration from item,  check if or default language is also there
                        var searchtoken = "," + defaultLanguage + ",";
                        if (altLanguages.ToLower().Contains(searchtoken))
                        {
                            //remove default from altLanguages, because it is the default.
                            altLanguages = altLanguages.Replace(searchtoken, ",");
                        }
                        else
                        {
                            //The first language is the default.
                            defaultLanguage = altLanguages.TrimStart(',').Split(',')[0];
                            altLanguages = altLanguages.Replace("," + defaultLanguage + ",", ",");
                        }
                    }
                }


                if (System.String.Compare(itemlanguage, defaultLanguage, System.StringComparison.OrdinalIgnoreCase) != 0)
                {
                    if (string.IsNullOrEmpty(altLanguages))
                    {
                        AddWarning(item, args, defaultLanguage, site.Name);
                        return;
                    }
                    else
                    {
                        if (!altLanguages.ToLower().Contains("," + itemlanguage.ToLower() + ","))
                        {
                            AddWarning(item, args, defaultLanguage + altLanguages, site.Name);
                            return;
                        }
                    }
                }
                CheckTranslations(item, args, defaultLanguage, altLanguages, site);
            }
            else
            {
                // item is not in content, not a website, so a system/ template / layout item.maby it is nice to see the "en" version
                ProcessNonSiteItem(item, args);
            }
        }

        private static void CheckTranslations(Item item, GetContentEditorWarningsArgs args, string language,
            string altLanguages, SiteInfo site)
        {
            //if no warning, maybe show somethings about not Translated Warning and fallback
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

        private static string GetAltLanguagesFromConfig(SiteInfo site)
        {
            //altLanguage is optional in site config may comma or | seperated. hostname also use the | mask.
            string altLanguages = site.Properties.Get("altLanguage");
            if (!string.IsNullOrEmpty(altLanguages))
            {
                altLanguages = "," + altLanguages.Trim().Replace(" ", "").Replace("|", ",") + ",";
            }

            return altLanguages;
        }

        private static string GetAltLanguagesFromRootItem(SiteInfo site, Item item)
        {
            //altlanguages may also in a field call "Languages" in the site root (not the home item, change this part if you want to use the home item or another item)
            var siteRootItem = item.Database.GetItem(site.RootPath);
            string altLanguages = string.Empty;
            if (siteRootItem != null)
            {
                Sitecore.Data.Fields.MultilistField languages = siteRootItem.Fields["Languages"];
                if (languages != null)
                {
                    var languageList = languages.GetItems();
                    foreach (var language in languageList)
                    {
                        altLanguages += "," + language.Name + ",";
                    }
                }
            }
            return altLanguages;
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
                        //limit to 4, but add en if precent because this is the default
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
                        (object)item.Language.GetDisplayName());
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