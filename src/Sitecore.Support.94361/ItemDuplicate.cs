
using Sitecore;
using Sitecore.Buckets.Extensions;
using Sitecore.Buckets.Managers;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.Globalization;
using Sitecore.Shell.Framework.Pipelines;
using Sitecore.Web.UI.Sheer;
using System;
namespace Sitecore.Support.Buckets.Pipelines.UI
{


  public class ItemDuplicate : DuplicateItem
  {
    public new void Execute(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      Database database = ExtractDatabase(args);
      string path = args.Parameters["id"];
      Item item = database.GetItem(path);
      if (item == null)
      {
        ShowLocalizedAlert("Item not found.", Array.Empty<object>());
        args.AbortPipeline();
      }
      else
      {
        Item parent = item.Parent;
        if (parent == null)
        {
          ShowLocalizedAlert("Cannot duplicate the root item.", Array.Empty<object>());
          args.AbortPipeline();
        }
        else if (parent.Access.CanCreate())
        {
          Log.Audit(this, "Duplicate item: {0}", AuditFormatter.FormatItem(item));
          Item parentBucketItemOrSiteRoot = GetParentBucketItemOrSiteRoot(item);
          if (IsBucket(parentBucketItemOrSiteRoot) && IsBucketable(item))
          {
            if (!EventDisabler.IsActive)
            {
              EventResult eventResult = Event.RaiseEvent("item:bucketing:duplicating", args, this);
              if (eventResult != null && eventResult.Cancel)
              {
                Log.Info(string.Format("Event {0} was cancelled", "item:bucketing:duplicating"), this);
                args.AbortPipeline();
                return;
              }
            }
            Item item2 = DuplicateItem(item, args.Parameters["name"]);
           // Item destination = CreateAndReturnBucketFolderDestination(parentBucketItemOrSiteRoot, DateUtil.ToUniversalTime(DateTime.Now), item);
            // if (!item.IsBucketTemplateCheck()) // Commenting out as this code does not have to compile for it is just for future use of this patch so people see what has changed uncomment the code for the real old implementation.
            // {
            //    destination = parentBucketItemOrSiteRoot;
            //   }
            //MoveItem(item2, destination);
            if (!EventDisabler.IsActive)
            {
              Event.RaiseEvent("item:bucketing:duplicated", args, this);
            }
          }
          else
          {
            DuplicateItem(item, args.Parameters["name"]);
          }
        }
        else
        {
          ShowLocalizedAlert("You do not have permission to duplicate \"{0}\".", item.DisplayName);
          args.AbortPipeline();
        }
      }
      args.AbortPipeline();
    }

    protected virtual void MoveItem(Item item, Item destination)
    {
      using (new StatisticDisabler())
      {
        ItemManager.MoveItem(item, destination);
      }
    }

    //  protected virtual Item CreateAndReturnBucketFolderDestination(Item bucketItem, DateTime createdDate, Item item) !!!!!!!!!!!!Uncomment this for real impl just commenting to compile quickly and commit original before changing to patch.
    //{
    //return BucketManager.CreateAndReturnBucketFolderDestination(bucketItem, createdDate, item);
    //}

    protected virtual Item GetParentBucketItemOrSiteRoot(Item item)
    {
      return item.GetParentBucketItemOrSiteRoot();
    }

    protected virtual Item DuplicateItem(Item item, string name)
    {
      return Context.Workflow.DuplicateItem(item, name);
    }

    protected virtual bool IsBucket(Item item)
    {
      return BucketManager.IsBucket(item);
    }

    protected virtual bool IsBucketable(Item item)
    {
      return BucketManager.IsBucketable(item);
    }

    protected virtual Database ExtractDatabase(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      Database database = Factory.GetDatabase(args.Parameters["database"]);
      Assert.IsNotNull(database, args.Parameters["database"]);
      return database;
    }

    protected virtual void ShowLocalizedAlert(string message, params object[] parameters)
    {
      SheerResponse.Alert(Translate.Text(message, parameters), Array.Empty<string>());
    }
  }

}