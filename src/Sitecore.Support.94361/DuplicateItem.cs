using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
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

namespace Sitecore.Support.Shell.Framework.Pipelines
{

  /// <summary>
  /// Represents the Duplicate pipeline.
  /// </summary>
  public class DuplicateItem
  {

    Database database;

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
      database = Factory.GetDatabase(args.Parameters["database"]);
      Assert.IsNotNull(database, args.Parameters["database"]);
      string value = args.Parameters["id"];
      Language result;
      if (!Language.TryParse(args.Parameters["language"],out result))
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
        {//Code in this body statement changed for patch, previously just called Context.Workflow.DuplicateItem(item, args.Parameters["name"]);
          Log.Audit(this, "Duplicate item: {0}", AuditFormatter.FormatItem(item));
          if (!item.Fields["__Default workflow"].Value.IsNullOrEmpty() && (item.Fields["__Workflow"].Value.IsNullOrEmpty() || item.Fields["__Workflow state"].Value.IsNullOrEmpty()))
          {
            Item i = item.Duplicate(args.Parameters["name"]);
            SetWorkflowsForItem(i);
            Context.Workflow.ResetWorkflowState(i, true);
            i.Locking.Unlock();
            if (i.Versions.Count > 0)
            {
              Context.Workflow.StartEditing(i);
            }
          }
          else
          {
            Context.Workflow.DuplicateItem(item, args.Parameters["name"]);
          }
        }
        else
        {
          SheerResponse.Alert(Translate.Text("You do not have permission to duplicate \"{0}\".", item.DisplayName), Array.Empty<string>());
          args.AbortPipeline();
        }
      }
    }

    //Code to change workflow from initial state after duplication from old patch.
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
