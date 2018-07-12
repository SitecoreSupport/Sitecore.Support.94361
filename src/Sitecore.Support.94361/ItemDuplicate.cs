using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.SecurityModel;
using Sitecore.StringExtensions;
using Sitecore.Web.UI.Sheer;
using Sitecore.Workflows;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sitecore.Support.Buckets.Pipelines.UI
{
  class ItemDuplicate : Sitecore.Buckets.Pipelines.UI.ItemDuplicate
  {

    Database database;
    protected override Item DuplicateItem(Item item, string name)
    {
      //Code in this method changed for patch, Previously just returned the Context.Workflow.DuplicateItem(item, name);
      if (!item.Fields["__Default workflow"].Value.IsNullOrEmpty() && (item.Fields["__Workflow"].Value.IsNullOrEmpty() || item.Fields["__Workflow state"].Value.IsNullOrEmpty()))
      {
        Item i = item.Duplicate(name);
        SetWorkflowsForItem(i);
        Context.Workflow.ResetWorkflowState(i, true);
        i.Locking.Unlock();
        if (i.Versions.Count > 0)
        {
          Context.Workflow.StartEditing(i);
        }
        return i;
      }
      else
      {
        return Context.Workflow.DuplicateItem(item, name);
      }
    }

    protected override Database ExtractDatabase(ClientPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");
      //Changed this to set the class variable and return instead of just return.
      database = Factory.GetDatabase(args.Parameters["database"]);
      Assert.IsNotNull(database, args.Parameters["database"]);
      return database;
    }

    //Methods from old patch to change workflow of item to initial state after duplication.
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



  }
}
