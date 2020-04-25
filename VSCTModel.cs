using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SystemEx;
using VSIXEx.Attributes;

namespace VSIXEx.Designer
{
	public class VSCTModel
	{
		protected Lazy<List<TypeAttributePair<IDSymbolsAttribute>>> IDSymbols;
		protected Lazy<Dictionary<Guid, GuidSymbolType>> GuidSymbols;
		protected Lazy<Dictionary<Guid, Dictionary<int, EnumNameValuePair<int>>>> CommandIDs;
		protected Lazy<Dictionary<Guid, IEnumerable<CommandType>>> CommandSets;
		protected Lazy<List<CommandGroupType>> CommandGroups;
		protected Lazy<List<CommandMenuType>> CommandMenus;
		protected Lazy<List<CommandButtonType>> CommandButtons;
		protected Lazy<List<CommandBitmapType>> CommandBitmaps;
		protected Lazy<List<KeyBindingType>> KeyBindings;

		public VSCTModel(Assembly assembly)
		{
			IDSymbols = new Lazy<List<TypeAttributePair<IDSymbolsAttribute>>>(() =>
				new[] { assembly, typeof(VsGuidSymbols).Assembly }
				.SelectMany(a => a.EnumIDSymbols())
				.ToList());
			GuidSymbols = new Lazy<Dictionary<Guid, GuidSymbolType>>(() =>
				new[] { assembly, typeof(VsGuidSymbols).Assembly }
				.SelectMany(a => a.EnumGuidSymbols())
				.ToDictionary(i => i.Guid));
			CommandIDs = new Lazy<Dictionary<Guid, Dictionary<int, EnumNameValuePair<int>>>>(() =>
				EnumIDSymbols()
				.GroupBy(id => id.Attribute.Guid)
				.Select(g => new { Guid = g.Key, IDs = g.SelectMany(gid => gid.Type.EnumCommandIds()).ToDictionary(i => i.Value) })
				.ToDictionary(i => i.Guid, i => i.IDs));
			CommandSets = new Lazy<Dictionary<Guid, IEnumerable<CommandType>>>(() =>
				assembly.EnumCommandSets()
				.SelectMany(ca => ca.EnumCommands())
				.GroupBy(i => i.Attribute.Guid)
				.ToDictionary(i => i.Key, i => i as IEnumerable<CommandType>));
			CommandGroups = new Lazy<List<CommandGroupType>>(() =>
				EnumIDSymbols()
				.Select(id => new { id.Attribute.Guid, IDs = id.Type.EnumEnumValuesWithAttribute<int, GroupAttribute>() })
				.SelectMany(id => id.IDs.Select(menu => new { id.Guid, Id = menu.Name, Group = menu.Attribute }))
				.Select(id => new CommandGroupType
				{
					Guid = GuidSymbols.Value[id.Guid].Name,
					Id = id.Id,
					Parent = new CommandParentType
					{
						Guid = GuidSymbols.Value[id.Group.Parent.FieldType.GetAttribute<IDSymbolsAttribute>().Guid].Name,
						Id = id.Group.Parent.Name,
					},
					Priority = id.Group.Priority
				})
				.ToList());
			CommandMenus = new Lazy<List<CommandMenuType>>(() =>
				EnumIDSymbols()
				.Select(id => new { id.Attribute.Guid, IDs = id.Type.EnumEnumValuesWithAttribute<int, BaseMenuAttribute>() })
				.SelectMany(id => id.IDs.Select(menu => new { id.Guid, Id = menu.Name, MenuAttribute = menu.Attribute }))
				.Select(id => new CommandMenuType
				{
					Guid = GuidSymbols.Value[id.Guid].Name,
					Id = id.Id,
					Parent = id.MenuAttribute.Parent != null
						? new CommandParentType
						{
							Guid = GuidSymbols.Value[id.MenuAttribute.Parent.FieldType.GetAttribute<IDSymbolsAttribute>().Guid].Name,
							Id = id.MenuAttribute.Parent.Name,
						}
						: CommandParentType.Empty,
					Type = id.MenuAttribute.Type,
					CommandFlag = id.MenuAttribute.CommandFlag,
					CommandName = id.MenuAttribute.CommandName,
					ButtonText = id.MenuAttribute.ButtonText,
				})
				.ToList());
			CommandButtons = new Lazy<List<CommandButtonType>>(() =>
				CommandSets.Value.SelectMany(i => i.Value.SelectMany(c => c.Buttons)
				.Select(btn => new CommandButtonType
				{
					Guid = GuidSymbols.Value[i.Key].Name,
					Id = CommandIDs.Value[i.Key][btn.Attribute.CommandId].Name,
					Type = btn.ButtonAttribute.Type,
					Priority = btn.ButtonAttribute.Priority,
					Parent = btn.ButtonAttribute.Parent != null
						? new CommandParentType
						{
							Guid = GuidSymbols.Value[btn.ButtonAttribute.Parent.FieldType.GetAttribute<IDSymbolsAttribute>().Guid].Name,
							Id = btn.ButtonAttribute.Parent.Name,
						}
						: CommandParentType.Empty,
					Icon = btn.ButtonAttribute.Icon != null
						? new CommandIconType
						{
							Guid = GuidSymbols.Value[btn.ButtonAttribute.Icon.FieldType.GetAttribute<IDSymbolsAttribute>().Guid].Name,
							Id = btn.ButtonAttribute.Icon.Name,
						}
						: CommandIconType.Empty,
					ButtonText = btn.ButtonAttribute.ButtonText
				}))
				.ToList());
			CommandBitmaps = new Lazy<List<CommandBitmapType>>(() =>
				EnumIDSymbols()
				.Select(id => new { id.Type, id.Attribute, BitmapAttribute = id.Type.GetAttribute<BitmapAttribute>() })
				.Where(id => id.BitmapAttribute != null)
				.Select(id => new CommandBitmapType
				{
					Guid = GuidSymbols.Value[id.Attribute.Guid].Name,
					Href = id.BitmapAttribute.Href,
					IDs = id.Type.EnumEnumValuesWithoutAttribute<int, NonSerializedAttribute>()
				})
				.ToList());
			KeyBindings = new Lazy<List<KeyBindingType>>(() =>
				CommandSets.Value.SelectMany(i => i.Value.SelectMany(c => c.KeyBindings)
				.Select(kb => new KeyBindingType
				{
					Guid = GuidSymbols.Value[i.Key].Name,
					Id = CommandIDs.Value[i.Key][kb.Attribute.CommandId].Name,
					Editor = "guidVSStd97",
					Attribute = kb.KeyBindingAttribute
				}))
				.ToList());
		}

		public Guid PackageGuid => GuidSymbols.Value
			.Where(s => s.Value.IsPackageGuid)
			.Select(s => s.Key)
			.First();
		public string PackageGuidName => GuidSymbols.Value
			.Where(s => s.Value.IsPackageGuid)
			.Select(s => s.Value.Name)
			.First();

		public IEnumerable<TypeAttributePair<IDSymbolsAttribute>> EnumIDSymbols() => IDSymbols.Value;

		public IEnumerable<CommandIDsType> EnumCommandIDs(bool withHidden = false)
		{
			return GuidSymbols.Value
				.Where(i => !i.Value.IsHidden || withHidden)
				.Select(i => new CommandIDsType
				{
					Guid = i.Key,
					Name = i.Value.Name,
					IDs = CommandIDs.Value.Where(ei => ei.Key == i.Key).Select(ei => ei.Value).SelectMany(ei => ei.Values)
				});
		}

		public IEnumerable<KeyBindingType> EnumKeyBindings() => KeyBindings.Value;
		public IEnumerable<CommandGroupType> EnumCommandGroups() => CommandGroups.Value;
		public IEnumerable<CommandButtonType> EnumCommandButtons() => CommandButtons.Value;
		public IEnumerable<CommandMenuType> EnumCommandMenus() => CommandMenus.Value;
		public IEnumerable<CommandBitmapType> EnumCommandBitmaps() => CommandBitmaps.Value;
	}
}
