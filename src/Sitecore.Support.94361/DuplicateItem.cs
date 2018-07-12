using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Web.UI.Sheer;
using System;

namespace Sitecore.Support.Shell.Framework.Pipelines
{
  /// <summary>
  /// Represents the Duplicate pipeline.
  /// </summary>
  public class DuplicateItem
  {
    /// <summary>
    /// Checks the permissions.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <contract>
    ///   <requires name="args" condition="not null" />
    /// </contract>
    public void CheckPermissions(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      if (SheerResponse.CheckModified())
      {
        Database database = Factory.GetDatabase(args.Parameters["database"]);
        Assert.IsNotNull(database, args.Parameters["database"]);
        string itemPath = args.Parameters["id"];
        Item item = database.Items[itemPath];
        if (item != null)
        {
          Item parent = item.Parent;
          if (parent != null)
          {
            if (!parent.Access.CanCreate())
            {
              SheerResponse.Alert(Translate.Text("You do not have permission to duplicate \"{0}\".", item.DisplayName), Array.Empty<string>());
              args.AbortPipeline();
            }
          }
          else
          {
            SheerResponse.Alert("Parent not found", Array.Empty<string>());
            args.AbortPipeline();
          }
        }
        else
        {
          SheerResponse.Alert("Item not found.", Array.Empty<string>());
          args.AbortPipeline();
        }
      }
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <contract>
    ///   <requires name="args" condition="not null" />
    /// </contract>
    public void GetName(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      Database database = Factory.GetDatabase(args.Parameters["database"]);
      Assert.IsNotNull(database, args.Parameters["database"]);
      string path = args.Parameters["id"];
      Item item = database.GetItem(path);
      if (item == null)
      {
        SheerResponse.Alert("Item not found.", Array.Empty<string>());
        args.AbortPipeline();
      }
      else if (item.Parent == null)
      {
        SheerResponse.Alert("Cannot duplicate the root item.", Array.Empty<string>());
        args.AbortPipeline();
      }
      else if (item.RuntimeSettings.IsVirtual && !item.Parent.RuntimeSettings.IsVirtual)
      {
        args.Parameters["name"] = item.Name;
        Context.ClientPage.ClientResponse.ClosePopups(true);
      }
      else if (args.IsPostBack)
      {
        if (args.HasResult)
        {
          string result = args.Result;
          if (item.TemplateID == TemplateIDs.Language && !IsValidLanguage(result))
          {
            SheerResponse.Input("Enter a name for the new item:", result, Settings.ItemNameValidation, "'$Input' is not a valid name.", Settings.MaxItemNameLength);
            args.WaitForPostBack();
          }
          else
          {
            args.Parameters["name"] = args.Result;
          }
        }
        else
        {
          args.AbortPipeline();
        }
      }
      else
      {
        string copyOfName = ItemUtil.GetCopyOfName(item.Parent, item.Name);
        SheerResponse.Input("Enter a name for the new item:", copyOfName, Settings.ItemNameValidation, "'$Input' is not a valid name.", Settings.MaxItemNameLength);
        args.WaitForPostBack();
      }
    }

    /// <summary>
    /// Executes the specified args.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <contract>
    ///   <requires name="args" condition="not null" />
    /// </contract>
    public void Execute(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      Database database = Factory.GetDatabase(args.Parameters["database"]);
      Assert.IsNotNull(database, args.Parameters["database"]);
      string value = args.Parameters["id"];
      if (!Language.TryParse(args.Parameters["language"], out Language result))
      {
        result = Context.Language;
      }
      Item item = database.GetItem(ID.Parse(value), result);
      if (item == null)
      {
        SheerResponse.Alert("Item not found.", Array.Empty<string>());
        args.AbortPipeline();
      }
      else
      {
        Item parent = item.Parent;
        if (parent == null)
        {
          SheerResponse.Alert("Cannot duplicate the root item.", Array.Empty<string>());
          args.AbortPipeline();
        }
        else if (parent.Access.CanCreate())
        {
          Log.Audit(this, "Duplicate item: {0}", AuditFormatter.FormatItem(item));
          Context.Workflow.DuplicateItem(item, args.Parameters["name"]);
        }
        else
        {
          SheerResponse.Alert(Translate.Text("You do not have permission to duplicate \"{0}\".", item.DisplayName), Array.Empty<string>());
          args.AbortPipeline();
        }
      }
    }

    /// <summary>
    /// Determines whether [is valid language] [the specified name].
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>
    /// 	<c>true</c> if [is valid language] [the specified name]; otherwise, <c>false</c>.
    /// </returns>
    private static bool IsValidLanguage(string name)
    {
      Assert.ArgumentNotNull(name, "name");
      if (LanguageManager.IsLanguageNameDefined(Context.ContentDatabase, name))
      {
        SheerResponse.Alert("The language is already defined in this database.", Array.Empty<string>());
        return false;
      }
      bool flag = true;
      try
      {
        Language.CreateCultureInfo(name);
      }
      catch
      {
        flag = false;
      }
      if (!flag)
      {
        SheerResponse.Alert($"The name \"{name}\" is not a valid or supported culture identifier.", Array.Empty<string>());
        return false;
      }
      return true;
    }
  }

}