using System;
using System.Collections.Generic;
using System.Reflection;
using SystemEx;
using VSIXEx.Attributes;
using VSIXEx.Designer.Templates;



namespace VSIXEx.Designer
{
	public struct GuidSymbolType
	{
		public Guid Guid;
		public string Name;
		public bool IsHidden;
		public bool IsPackageGuid;
	}

	public struct CommandIDsType
	{
		public Guid Guid;
		public string Name;
		public IEnumerable<EnumNameValuePair<int>> IDs;
	}

	public struct KeyBindingType
	{
		public string Guid;
		public string Id;
		public string Editor;

		public KeyBindingAttribute Attribute;
	}

	public struct CommandParentType
	{
		public static CommandParentType Empty = new CommandParentType { Guid = null, Id = null };
		public bool IsEmpty { get => Guid == null || Id == null; }

		public string Guid;
		public string Id;
	}

	public struct CommandGroupType
	{
		public string Guid;
		public string Id;
		public CommandParentType Parent;
		public int Priority;
	}

	public struct CommandIconType
	{
		public static CommandIconType Empty = new CommandIconType { Guid = null, Id = null };
		public bool IsEmpty { get => Guid == null || Id == null; }

		public string Guid;
		public string Id;
	}

	public struct CommandMenuType
	{
		public string Guid;
		public string Id;
		public CommandParentType Parent;
		public MenuType Type;
		public MenuCommandFlag CommandFlag;
		public string ButtonText;
		public string CommandName;
	}

	public struct CommandButtonType
	{
		public string Guid;
		public string Id;
		public int Priority;
		public ButtonType Type;
		public CommandParentType Parent;
		public CommandIconType Icon;
		public string ButtonText;
	}

	public struct CommandBitmapType
	{
		public string Guid;
		public string Href;
		public IEnumerable<EnumNameValuePair<int>> IDs;
	}

	public static class VSCTEx
	{
		public static IEnumerable<TypeAttributePair<IDSymbolsAttribute>> EnumIDSymbols(this Assembly assembly)
			=> assembly.EnumTypesWithAttribute<IDSymbolsAttribute>();

		public static IEnumerable<GuidSymbolType> EnumGuidSymbols(this Assembly assembly)
		{
			foreach (var type in assembly.EnumTypesWithAttribute<GuidSymbolsAttribute>())
			{
				foreach (var field in (type.Type as Type).EnumFieldsWithAttribute<GuidSymbolAttribute>(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
				{
					yield return new GuidSymbolType
					{
						Guid = new Guid(field.Field.GetValue(null) as string),
						Name = field.Attribute.GetName(field.Field),
						IsHidden = field.Attribute.Hidden,
						IsPackageGuid = field.Field.HasAttribute<PackageGuidSymbolAttribute>()
					};
				}
			}
		}

		public static IEnumerable<EnumNameValuePair<int>> EnumCommandIds(this Type type)
			=> type.EnumEnumValuesWithoutAttribute<int, NonSerializedAttribute>();


		public static string GenerateIdSymbolStrings(this Assembly assembly)
			=> Template.TransformToText<VsixIdSymbolStrings>(new { assembly }.ToExpando());

		public static string GenerateKeyBindings(this VSCTModel model)
			=> Template.TransformToText<VsctKeyBindings>(new { model }.ToExpando());

		public static string GenerateSymbols(this VSCTModel model)
			=> Template.TransformToText<VsctSymbols>(new { model }.ToExpando());

		public static string GenerateCommandGroups(this VSCTModel model)
			=> Template.TransformToText<VsctCommandsGroups>(new { model }.ToExpando());

		public static string GenerateCommandMenus(this VSCTModel model)
			=> Template.TransformToText<VsctCommandsMenus>(new { model }.ToExpando());

		public static string GenerateCommandButtons(this VSCTModel model)
			=> Template.TransformToText<VsctCommandsButtons>(new { model }.ToExpando());

		public static string GenerateCommandBitmaps(this VSCTModel model)
			=> Template.TransformToText<VsctCommandsBitmaps>(new { model }.ToExpando());
	}
}
