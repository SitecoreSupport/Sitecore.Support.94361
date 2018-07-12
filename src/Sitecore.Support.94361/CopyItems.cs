using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.SecurityModel;
using Sitecore.Shell.Framework.Pipelines;
using Sitecore.StringExtensions;
using Sitecore.Workflows;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sitecore.Support.Shell.Framework.Pipelines
{
  class CopyItems : Sitecore.Shell.Framework.Pipelines.CopyItems
  {

    private Database database;
    public override void Execute(CopyItemsArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      database = GetDatabase(args);
      Item item = database.GetItem(args.Parameters["destination"], Language.Parse(args.Parameters["language"]));
      Assert.IsNotNull(item, args.Parameters["destination"]);
      ArrayList arrayList = new ArrayList();
      List<Item> items = GetItems(args);
      foreach (Item item2 in items)
      {
        if (item2 != null)//Changed this if statement body to check for default workflow with no state or workflow, previously it was just adding Context.Workflow.CopyItem(item2,item,copyOfName) to the array list.
        {
          string copyOfName = ItemUtil.GetCopyOfName(item, item2.Name);
          Item item3 = null;
          if (!item2.Fields["__Default workflow"].Value.IsNullOrEmpty() && (item2.Fields["__Workflow"].Value.IsNullOrEmpty() || item2.Fields["__Workflow state"].Value.IsNullOrEmpty()))
          {
            item3 = item2.CopyTo(item, copyOfName, ID.NewID, true);
            SetWorkflowsForItem(item3);
            Context.Workflow.ResetWorkflowState(item3, true);
            item3.Locking.Lock();
            if (item3.Versions.Count > 0)
            {
              Context.Workflow.StartEditing(item3);
            }
          }
          else
          {
            item3 = Context.Workflow.CopyItem(item2, item, copyOfName);
          }
          arrayList.Add(item3);
        }
      }
      args.Copies = (arrayList.ToArray(typeof(Item)) as Item[]);
    }

    //Methods from old patch that still work to set the workflow to the initial state after it is copied.
    private IWorkflow GetDefaultWorkflow(Item item)
    {
      if (item != null)
      {
        return GetWorkflow(item.Fields["__Default workflow"].Value);
      }
      return null;
    }

    private IWorkflow GetWorkflow(string wfId)
    {
      if (!wfId.IsNullOrEmpty() && database.GetItem(wfId) != null)
      {
        return database.WorkflowProvider.GetWorkflow(wfId);
      }
      return null;
    }

    private void SetWorkflowsForItem(Item source)
    {
      IWorkflow defaultWorkflow = GetDefaultWorkflow(source);
      IEnumerable versions = source.Versions.GetVersions(true);
      foreach (Item item in versions)
      {
        if (defaultWorkflow != null)
        {
          using (new SecurityDisabler())
          {
            using (new EditContext(item))
            {
              item.Fields["__Workflow"].Value = defaultWorkflow.WorkflowID;
              item.Fields["__Workflow state"].Value = database.GetItem(defaultWorkflow.WorkflowID).Fields["Initial state"].Value;
            }
          }
        }
      }
    }
  }
}
