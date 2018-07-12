
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Shell.Framework.Pipelines;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;
using System.Collections;
using System.Collections.Generic;
namespace Sitecore.Support.Shell.Framework.Pipelines
{

  /// <summary>
  /// Represents the Copy Items UI pipeline.
  /// </summary>
  public class CopyItems
  {
    /// <summary>
    /// Gets the destination.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <contract>
    ///   <requires name="args" condition="not null" />
    /// </contract>
    public void GetDestination(CopyItemsArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      if (SheerResponse.CheckModified())
      {
        Database database = GetDatabase(args);
        ListString listString = new ListString(args.Parameters["items"], '|');
        Item item = database.Items[listString[0]];
        UrlString urlString = new UrlString(GetDialogUrl());
        if (item != null)
        {
          urlString.Append("fo", item.ID.ToString());
          urlString.Append("sc_content", item.Database.Name);
          urlString.Append("la", args.Parameters["language"]);
        }
        Context.ClientPage.ClientResponse.ShowModalDialog(urlString.ToString(), "1200px", "700px", string.Empty, true);
        args.WaitForPostBack(false);
      }
    }

    /// <summary>
    /// Gets the dialog URL.
    /// </summary>
    /// <returns>The path to dialog URL.</returns>
    protected virtual string GetDialogUrl()
    {
      return "/sitecore/shell/Applications/Dialogs/Copy to.aspx";
    }

    /// <summary>
    /// Checks the language.
    /// </summary>
    /// <param name="args">The arguments.</param>
    public void CheckDestination(CopyItemsArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      if (!args.HasResult)
      {
        args.AbortPipeline();
      }
      else
      {
        Database database = GetDatabase(args);
        if (args.Result != null && args.Result.Length > 0 && args.Result != "undefined")
        {
          Item item = database.GetItem(args.Result);
          if (!item.Access.CanCreate())
          {
            Context.ClientPage.ClientResponse.Alert("You do not have permission to create items here.");
            args.AbortPipeline();
            return;
          }
          args.Parameters["destination"] = args.Result;
        }
        args.IsPostBack = false;
      }
    }

    /// <summary>
    /// Checks the language.
    /// </summary>
    /// <param name="args">The arguments.</param>
    public void CheckLanguage(CopyItemsArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      if (args.IsPostBack)
      {
        if (args.Result != "yes")
        {
          args.AbortPipeline();
        }
      }
      else
      {
        bool flag = false;
        List<Item> items = GetItems(args);
        foreach (Item item in items)
        {
          if (item.TemplateID == TemplateIDs.Language)
          {
            flag = true;
            break;
          }
        }
        if (flag)
        {
          SheerResponse.Confirm("You are coping a language.\n\nA language item must have a name that is a valid ISO-code.\n\nPlease rename the copied item afterward.\n\nAre you sure you want to continue?");
          args.WaitForPostBack();
        }
      }
    }

    /// <summary>
    /// Executes the specified args.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <contract>
    ///   <requires name="args" condition="not null" />
    /// </contract>
    public virtual void Execute(CopyItemsArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      Database database = GetDatabase(args);
      Item item = database.GetItem(args.Parameters["destination"], Language.Parse(args.Parameters["language"]));
      Assert.IsNotNull(item, args.Parameters["destination"]);
      ArrayList arrayList = new ArrayList();
      List<Item> items = GetItems(args);
      foreach (Item item2 in items)
      {
        if (item2 != null)
        {
          arrayList.Add(CopyItem(item, item2));
        }
      }
      args.Copies = (arrayList.ToArray(typeof(Item)) as Item[]);
    }

    /// <summary>
    /// Copies the item.
    /// </summary>
    /// <param name="target">The target item.</param>
    /// <param name="itemToCopy">The item to copy.</param>
    /// <returns>Returns copy of item.</returns>
    protected virtual Item CopyItem(Item target, Item itemToCopy)
    {
      Assert.ArgumentNotNull(target, "target");
      Assert.ArgumentNotNull(itemToCopy, "itemToCopy");
      string text = target.Uri.ToString();
      string copyOfName = ItemUtil.GetCopyOfName(target, itemToCopy.Name);
      Item item = Context.Workflow.CopyItem(itemToCopy, target, copyOfName);
      Log.Audit(this, "Copy item from: {0} to {1}", AuditFormatter.FormatItem(itemToCopy), AuditFormatter.FormatItem(item), text);
      return item;
    }

    /// <summary>
    /// Gets the database.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <returns>The database.</returns>
    protected static Database GetDatabase(CopyItemsArgs args)
    {
      string text = args.Parameters["database"];
      Database database = Factory.GetDatabase(text);
      Assert.IsNotNull(database, text);
      return database;
    }

    /// <summary>
    /// Gets the items.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <returns>The items.</returns>
    protected static List<Item> GetItems(CopyItemsArgs args)
    {
      List<Item> list = new List<Item>();
      Database database = GetDatabase(args);
      ListString listString = new ListString(args.Parameters["items"], '|');
      foreach (string item2 in listString)
      {
        Item item = database.GetItem(item2, Language.Parse(args.Parameters["language"]));
        if (item != null)
        {
          list.Add(item);
        }
      }
      return list;
    }
  }

}