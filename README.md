Sitecore-Language-Editor-Warning
================================

With this Sitecore module you get a content editor warning when the wrong language is selected for the current website. An also a user friendly button to switch the language.

![Example of Belgian website](https://raw.githubusercontent.com/jbluemink/Sitecore-Language-Editor-Warning/master/ContentEditorWarningBE.PNG)

##Download Sitecore Package:
A compiled and packaged version is available on the [Sitecore Marketplace](https://marketplace.sitecore.net/en/Modules/Sitecore_Language_Content_Editor_Warning.aspx)

##To Build:
Add Sitecore.Kernel.dll to External folder

##Deploy:
- Copy the builded Language.Editor.Warning.dll to the Sitecore bin directory
- Register the Content Editor Warning. place the Language.Editor.Warning.config in the Sitecore App_Config/include


##Explanation
With this module you get a content editor warning when the wrong language is selected.

On multilanguage and multisite Sitecore environments and also on sites with another language than Default "en". There are typical users with rights on more than one language.
A common mistake is create or edit content items in the wrong language.

This language content editor warning will prevent you for editing in the wrong language and with links to the correct language it makes the CMS user-friendly.

Each site in Sitecore has optional a default language. define in the &lt;sites&gt;&lt;site&gt; node. For example:
&lt;site name="website" language="nl-NL" ...

On Multilanguage Site’s you can use a custom attribute altLanguage. It's possible to set more than one alternative language by using '|' symbol as a separator, same as for the host.
&lt;site name="website" language="nl-BE" altLanguage="fr-BE|en" ...

Read more on [Sitecore Stockpick](http://sitecore.stockpick.nl/) or the Dutch artikel [Sitecore editen in de juiste taal](http://sitecore.stockpick.nl/nederlands/editen-in-de-juiste-taal.aspx)
