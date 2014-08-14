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


        private static void GetWebsite(Item item, GetContentEditorWarningsArgs args)
        {
            var path = item.Paths.FullPath;
            var itemlanguage = item.Language.ToString();
            foreach (var site in global::Sitecore.Configuration.Settings.Sites)
            {
                if (path.StartsWith(site.RootPath) && site.Name != "shell" && site.Name != "modules_shell" && site.Name != "modules_website" && site.RootPath.Trim() != string.Empty)
                {
                    var language = site.Language;
                    if (string.IsNullOrEmpty(language))
                    {
                        //language attribuut is optioneel, is die er niet gebruik dan de default language.
                        language = Sitecore.Configuration.Settings.DefaultLanguage;
                    }
                    if (System.String.Compare(itemlanguage, language, System.StringComparison.OrdinalIgnoreCase) != 0)
                    {
                        string altLanguages = site.Properties.Get("altLanguage");  //altLanguage is optioneel en mag comma of | seperated zijn.
                        
                        if (string.IsNullOrEmpty(altLanguages))
                        {
                            AddWarning(item, args, language);
                        }
                        else
                        {
                            altLanguages = "," + altLanguages.Trim().Replace(" ", "").Replace("|", ",") + ",";
                            if (!altLanguages.Contains("," + itemlanguage + ","))
                            {
                                AddWarning(item, args, language + altLanguages);
                            }
                        }
                    }
                }
            }
        }
 
        /// <summary>
        ///
        /// </summary>
        /// <param name="item"></param>
        /// <param name="args"></param>
        /// <param name="language">a comma separated list</param>
        public static void AddWarning(Item item, GetContentEditorWarningsArgs args, string language)
        {
            GetContentEditorWarningsArgs.ContentEditorWarning warning = args.Add();
 
            warning.Title = "You are not in the default language of the current website";
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
    }
}