using System;
using System.Xml.Linq;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Layouts;
using Sitecore.Links;
using Sitecore.Pipelines;
using Sitecore.Pipelines.ResolveRenderingDatasource;
using Sitecore.Text;

namespace Sitecore.Support.Data.Fields
{
  public class LayoutField : Sitecore.Data.Fields.LayoutField
  {
    public LayoutField(Item item) : base(item)
    {
    }

    public LayoutField(Field innerField) : base(innerField)
    {
    }

    public LayoutField(Field innerField, string runtimeValue) : base(innerField, runtimeValue)
    {
    }

    public override void Relink(ItemLink itemLink, Item newLink)
    {
      Assert.ArgumentNotNull(itemLink, "itemLink");
      Assert.ArgumentNotNull(newLink, "newLink");
      var str = Value;
      if (!string.IsNullOrEmpty(str))
      {
        var definition = LayoutDefinition.Parse(str);
        var devices = definition.Devices;
        if (devices != null)
        {
          var b = itemLink.TargetItemID.ToString();
          var str3 = newLink.ID.ToString();
          for (var i = devices.Count - 1; i >= 0; i--)
          {
            var definition2 = devices[i] as DeviceDefinition;
            if (definition2 != null)
              if (definition2.ID == b)
              {
                definition2.ID = str3;
              }
              else if (definition2.Layout == b)
              {
                definition2.Layout = str3;
              }
              else
              {
                if (definition2.Placeholders != null)
                {
                  var targetPath = itemLink.TargetPath;
                  var flag = false;
                  for (var j = definition2.Placeholders.Count - 1; j >= 0; j--)
                  {
                    var definition3 = definition2.Placeholders[j] as PlaceholderDefinition;
                    if ((definition3 != null) &&
                        (string.Equals(definition3.MetaDataItemId, targetPath,
                           StringComparison.InvariantCultureIgnoreCase) ||
                         string.Equals(definition3.MetaDataItemId, b, StringComparison.InvariantCultureIgnoreCase)))
                    {
                      definition3.MetaDataItemId = newLink.Paths.FullPath;
                      flag = true;
                    }
                  }
                  if (flag)
                    continue;
                }
                if (definition2.Renderings != null)
                  for (var k = definition2.Renderings.Count - 1; k >= 0; k--)
                  {
                    var definition4 = definition2.Renderings[k] as RenderingDefinition;
                    if (definition4 != null)
                    {
                      if (definition4.ItemID == b)
                        definition4.ItemID = str3;
                      if (definition4.Datasource == b)
                        definition4.Datasource = str3;
                      if (definition4.Datasource == itemLink.TargetPath)
                        definition4.Datasource = newLink.Paths.FullPath;
                      if (!string.IsNullOrEmpty(definition4.Parameters))
                      {
                        var layoutItem = InnerField.Database.GetItem(definition4.ItemID);
                        if (layoutItem == null)
                          continue;
                        var parametersFields = GetParametersFields(layoutItem, definition4.Parameters);
                        foreach (var field in parametersFields.Values)
                          if (!string.IsNullOrEmpty(field.Value))
                            field.Relink(itemLink, newLink);
                        definition4.Parameters = parametersFields.GetParameters().ToString();
                      }
                      if (definition4.Rules != null)
                      {
                        var field2 = new RulesField(InnerField, definition4.Rules.ToString());
                        field2.Relink(itemLink, newLink);
                        definition4.Rules = XElement.Parse(field2.Value);
                      }
                    }
                  }
              }
          }
          Value = definition.ToXml();
        }
      }
    }

    public override void ValidateLinks(LinksValidationResult result)
    {
      Assert.ArgumentNotNull(result, "result");
      var str = Value;
      if (!string.IsNullOrEmpty(str))
      {
        var devices = LayoutDefinition.Parse(str).Devices;
        if (devices != null)
          foreach (DeviceDefinition definition2 in devices)
          {
            if (!string.IsNullOrEmpty(definition2.ID))
            {
              var targetItem = InnerField.Database.GetItem(definition2.ID);
              if (targetItem != null)
                result.AddValidLink(targetItem, definition2.ID);
              else
                result.AddBrokenLink(definition2.ID);
            }
            if (!string.IsNullOrEmpty(definition2.Layout))
            {
              var item = InnerField.Database.GetItem(definition2.Layout);
              if (item != null)
                result.AddValidLink(item, definition2.Layout);
              else
                result.AddBrokenLink(definition2.Layout);
            }
            ValidatePlaceholderSettings(result, definition2);
            if (definition2.Renderings != null)
              foreach (RenderingDefinition definition3 in definition2.Renderings)
                if (definition3.ItemID != null)
                {
                  var item3 = InnerField.Database.GetItem(definition3.ItemID);
                  if (item3 != null)
                    result.AddValidLink(item3, definition3.ItemID);
                  else
                    result.AddBrokenLink(definition3.ItemID);
                  var datasource = definition3.Datasource;
                  if (!string.IsNullOrEmpty(datasource))
                  {
                    using (new ContextItemSwitcher(InnerField.Item))
                    {
                      var args = new ResolveRenderingDatasourceArgs(datasource);
                      CorePipeline.Run("resolveRenderingDatasource", args, false);
                      datasource = args.Datasource;
                    }
                    var item4 = InnerField.Database.GetItem(datasource);
                    if (item4 != null)
                      result.AddValidLink(item4, datasource);
                    else if (!datasource.Contains(":"))
                      result.AddBrokenLink(datasource);
                  }
                  var multiVariateTest = definition3.MultiVariateTest;
                  if (!string.IsNullOrEmpty(multiVariateTest))
                  {
                    var item5 = InnerField.Database.GetItem(multiVariateTest);
                    if (item5 != null)
                      result.AddValidLink(item5, multiVariateTest);
                    else
                      result.AddBrokenLink(multiVariateTest);
                  }
                  var personalizationTest = definition3.PersonalizationTest;
                  if (!string.IsNullOrEmpty(personalizationTest))
                  {
                    var item6 = InnerField.Database.GetItem(personalizationTest);
                    if (item6 != null)
                      result.AddValidLink(item6, personalizationTest);
                    else
                      result.AddBrokenLink(personalizationTest);
                  }
                  if ((item3 != null) && !string.IsNullOrEmpty(definition3.Parameters))
                    foreach (var field in GetParametersFields(item3, definition3.Parameters).Values)
                      field.ValidateLinks(result);
                  if (definition3.Rules != null)
                    new RulesField(InnerField, definition3.Rules.ToString()).ValidateLinks(result);
                }
          }
      }
    }


    public override void RemoveLink(ItemLink itemLink)
    {
      Assert.ArgumentNotNull(itemLink, "itemLink");
      var str = Value;
      if (!string.IsNullOrEmpty(str))
      {
        var definition = LayoutDefinition.Parse(str);
        var devices = definition.Devices;
        if (devices != null)
        {
          var b = itemLink.TargetItemID.ToString();
          for (var i = devices.Count - 1; i >= 0; i--)
          {
            var definition2 = devices[i] as DeviceDefinition;
            if (definition2 != null)
              if (definition2.ID == b)
              {
                devices.Remove(definition2);
              }
              else if (definition2.Layout == b)
              {
                definition2.Layout = null;
              }
              else
              {
                if (definition2.Placeholders != null)
                {
                  var targetPath = itemLink.TargetPath;
                  var flag = false;
                  for (var j = definition2.Placeholders.Count - 1; j >= 0; j--)
                  {
                    var definition3 = definition2.Placeholders[j] as PlaceholderDefinition;
                    if ((definition3 != null) &&
                        (string.Equals(definition3.MetaDataItemId, targetPath,
                           StringComparison.InvariantCultureIgnoreCase) ||
                         string.Equals(definition3.MetaDataItemId, b, StringComparison.InvariantCultureIgnoreCase)))
                    {
                      definition2.Placeholders.Remove(definition3);
                      flag = true;
                    }
                  }
                  if (flag)
                    continue;
                }
                if (definition2.Renderings != null)
                  for (var k = definition2.Renderings.Count - 1; k >= 0; k--)
                  {
                    var definition4 = definition2.Renderings[k] as RenderingDefinition;
                    if (definition4 != null)
                    {
                      if (definition4.Datasource == itemLink.TargetPath)
                        definition4.Datasource = string.Empty;
                      if (definition4.ItemID == b)
                        definition2.Renderings.Remove(definition4);
                      if (definition4.Datasource == b)
                        definition4.Datasource = string.Empty;
                      if (!string.IsNullOrEmpty(definition4.Parameters))
                      {
                        var layoutItem = InnerField.Database.GetItem(definition4.ItemID);
                        if (layoutItem == null)
                          continue;
                        var parametersFields = GetParametersFields(layoutItem, definition4.Parameters);
                        foreach (var field in parametersFields.Values)
                          if (!string.IsNullOrEmpty(field.Value))
                            field.RemoveLink(itemLink);
                        definition4.Parameters = parametersFields.GetParameters().ToString();
                      }
                      if (definition4.Rules != null)
                      {
                        var field2 = new RulesField(InnerField, definition4.Rules.ToString());
                        field2.RemoveLink(itemLink);
                        definition4.Rules = XElement.Parse(field2.Value);
                      }
                    }
                  }
              }
          }
          Value = definition.ToXml();
        }
      }
    }

    private RenderingParametersFieldCollection GetParametersFields(Item layoutItem, string renderingParameters)
    {
      RenderingParametersFieldCollection fields;
      var parameters = new UrlString(renderingParameters);
      RenderingParametersFieldCollection.TryParse(layoutItem, parameters, out fields);
      return fields;
    }
  }
}