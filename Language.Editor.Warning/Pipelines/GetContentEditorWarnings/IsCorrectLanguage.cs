using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Sitecore.Collections;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Globalization;
using Language = Sitecore.Globalization.Language;
using Sitecore.Pipelines.GetContentEditorWarnings;
using Sitecore.Web;
using Version = Sitecore.Data.Version;

namespace Stockpick.LanguageWarning.Pipelines.GetContentEditorWarnings
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

        public static Item GetLanguageVersionIfExist(Item item, Sitecore.Globalization.Language language)
        {
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
            // for this, this works beter than Sitecore.Links.LinkManager.ResolveTargetSite
        }

        private static void Check(Item item, GetContentEditorWarningsArgs args)
        {
            var path = item.Paths.FullPath;
            if (path.StartsWith("/sitecore/content/"))
            {
                var itemlanguage = item.Language;
                var site = GetSiteInfo(item);
                if (site.Name == "shell")
                {
                    //ignore no content site found for this item.
                    return;
                }
                var altlanguages = GetAltLanguagesFromConfig(site);
                var languages = new List<Language>();
                if (!altlanguages.Any())
                {
                    //no altlanguage sitconfig found try site root item.
                    languages = GetLanguagesFromRootItem(site, item);
                } 
                 
                if (!languages.Any())
                {
                    //no language configured, use the default language
                    //Or altlanguage in config add the default laguage to the list
                    var defaultLanguage = site.Language;
                    if (string.IsNullOrEmpty(defaultLanguage))
                    {
                        //language attribute is optional, if not present use default language.
                        defaultLanguage = Sitecore.Configuration.Settings.DefaultLanguage;
                    }
                    languages.Add(LanguageManager.GetLanguage(defaultLanguage));
                }
                if (altlanguages.Any())
                {
                    languages.AddRange(altlanguages);
                }
                if (!languages.Contains(itemlanguage))
                {
                    AddWarning(item, args, languages, site.Name);
                    return;
                }
                CheckTranslations(item, args, languages, site);
            }
            else
            {
                // item is not in content, not a website, so a system/ template / layout item. maybe it is nice to see the "en" version
                ProcessNonSiteItem(item, args);
            }
        }


        private static IList<Language> GetAltLanguagesFromConfig(SiteInfo site)
        {
            //altLanguage is optional in site config may comma or | seperated. hostname also use the | mask.
            string altLanguages = site.Properties.Get("altLanguage");
            var languages = new List<Language>();
            if (!string.IsNullOrEmpty(altLanguages))
            {
                altLanguages = altLanguages.Trim().Replace(" ", "").Replace("|", ",");
                foreach(var language in altLanguages.Split(','))
                {
                    var sitLanguage = Sitecore.Globalization.Language.Parse(language);
                    if (sitLanguage != null)
                    {
                        languages.Add(sitLanguage);
                    }
                }

            }

            return languages;
        }

     
        //check or translation exsist and also check the Item Fallback version.
        private static void CheckTranslations(Item item, GetContentEditorWarningsArgs args, IList<Sitecore.Globalization.Language> languages, SiteInfo site)
        {
            //if no warning, maybe show somethings about not Translated Warning and fallback
            var versionnotfound = new List<Sitecore.Globalization.Language>();
            var fallbackfound = new List<Item>();
            foreach (var language in languages)
            {
                var lanItem = GetLanguageVersionIfExist(item, language);
                if (lanItem == null)
                {
                    versionnotfound.Add(language);
                }
                else if (lanItem.Language != lanItem.OriginalLanguage)
                {
                    fallbackfound.Add(lanItem);
                }
            }

            if (versionnotfound.Any() || fallbackfound.Any())
            {
                AddTranslateWarning(item, args, versionnotfound, fallbackfound, site.Name);
            }
        }

        private static List<Language> GetLanguagesFromRootItem(SiteInfo site, Item item)
        {
            //languages may also in a field call "Languages" in the site root (not the home item, change this part if you want to use the home item or another item)
            var siteRootItem = item.Database.GetItem(site.RootPath);
            var languageList = new List<Sitecore.Globalization.Language>();
            if (siteRootItem != null)
            {
                Sitecore.Data.Fields.MultilistField languages = siteRootItem.Fields["Languages"];
                if (languages != null)
                {
                    var languageItemList = languages.GetItems();
                    foreach (var languageItem in languageItemList)
                    {
                        languageList.Add(LanguageManager.GetLanguage(languageItem.Name));
                    }
                }
            }
            return languageList;
        }

        public static void AddWarning(Item item, GetContentEditorWarningsArgs args, IList<Language> languages, string sitename)
        {
            GetContentEditorWarningsArgs.ContentEditorWarning warning = args.Add();

            warning.Title = "You are not in the language of the current site: " + sitename;
            warning.Text = "Switch to the correct language";
            foreach (var language in languages)
            {
                if (language != null)
                {
                    warning.AddOption(string.Format("Switch to {0}", language.GetDisplayName()),
                            string.Format(CultureInfo.InvariantCulture, "item:load(id={0},language={1})", item.ID,
                                language.Name));
                }
            }
            warning.IsExclusive = true;
        }

        private static void AddTranslateWarning(Item item, GetContentEditorWarningsArgs args, IList<Sitecore.Globalization.Language> notranslation, IList<Item> fallback, string sitename)
        {
            GetContentEditorWarningsArgs.ContentEditorWarning warning = args.Add();
            warning.Title = "This item is not translated for the site: " + sitename;
            warning.Text = "Switch to the not translated language and create a version";

            foreach (var language in notranslation)
            {
                warning.AddOption(string.Format("Switch to {0}", language.GetDisplayName()), string.Format(CultureInfo.InvariantCulture, "item:load(id={0},language={1})", item.ID, language.Name));
            }

            if (fallback.Any())
            {
                foreach (var languageitem in fallback)
                {
                    warning.AddOption(string.Format("Switch to {0} (now uses {1} language fallback)", languageitem.Language.GetDisplayName(), languageitem.OriginalLanguage.GetDisplayName()), string.Format(CultureInfo.InvariantCulture, "item:load(id={0},language={1})", item.ID, languageitem.Language.Name));
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
            foreach (Language language in languages)
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