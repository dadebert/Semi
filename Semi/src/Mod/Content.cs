﻿using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Semi {
	public abstract partial class Mod : MonoBehaviour {
		/// <summary>
		/// Name of the mod resources directory.
		/// </summary>
		public const string RESOURCES_DIR_NAME = "resources";

		internal bool RegisteringMode;
		internal string ResourcePath {
			get { return Path.Combine(Info.Path, RESOURCES_DIR_NAME); }
		}

		private static char[] _SeparatorSplitArray = { '\\', '/' };

		/// <summary>
		/// Normalize a path, removing all '..' entries. This is used to avoid filesystem access outside of the resources directory in Semi methods.
		/// Note that this does not make mods secure, as there are still other ways that you could access the filesystem (for example, by directly using the <c>System.IO</c> APIs).
		/// </summary>
		/// <returns>The normalized path.</returns>
		/// <param name="path">Path.</param>
		public static string NormalizePath(string path) {
			var split = path.ToLowerInvariant().Split(_SeparatorSplitArray, StringSplitOptions.RemoveEmptyEntries);
			var list = new List<string>();

			for (int i = 0; i < split.Length; i++) {
				var el = split[i];

				if (el == "..") {
					if (list.Count > 0) list.RemoveAt(list.Count - 1);
				} else if (el != ".") {
					list.Add(el);
				}
			}

			return string.Join("/", list.ToArray());
		}

		/// <summary>
		/// Expands a path relative to the mod resources directory into an absolute path.
		/// Additionally, it will convert forward slash directory separators into backward slashes on Windows.
		/// Resource paths can only use forward slashes - this method will throw if it detects a backward slash.
		/// </summary>
		/// <returns>The full resource path.</returns>
		/// <param name="res_path">Relative resource path.</param>
		public string GetFullResourcePath(string res_path) {
			if (res_path.Contains("\\")) throw new ArgumentException($"Mod resource paths cannot use backward slashes ('\\') to separate directories. Please use forward slashes ('/'), they will get converted appropriately depending on the current operating system.");
			if (Path.PathSeparator == '\\') res_path = res_path.Replace('/', '\\');
			return Path.Combine(ResourcePath, res_path);
		}

		/// <summary>
		/// Expands an ID, filling in context namespace if necessary.
		/// </summary>
		/// <returns>The full ID including the namespace.</returns>
		/// <param name="id">The ID.</param>
		/// <param name="require_local">Whether this ID must point to the mod's namespace.</param>
		public string GetFullID(string id, bool require_local) {
			var localization_key = false;
			if (id.StartsWithInvariant("#")) {
				localization_key = true;
				id = id.Substring(1);
			}

			id = IDPool<bool>.Resolve(id, Config.ID);
			if (require_local && IDPool<bool>.Split(id).Namespace != Config.ID) throw new Exception($"ID must point to the mod's namespace: '{id}'");

			if (localization_key) {
				id = $"#{id}";
			}

			return id;
		}

		internal void CheckMode() {
			if (!RegisteringMode) throw new InvalidOperationException($"Content can only be registered in the RegisterContent method.");
		}

		/// <summary>
		/// Creates a single sprite definition from an image resource.
		/// </summary>
		/// <returns>The sprite definition.</returns>
		/// <param name="id">ID (including the mod's namespace) for the new sprite definition.</param>
		/// <param name="sprite_path">Relative resource path to the image.</param>
		public SpriteDefinition CreateSpriteDefinition(string id, string sprite_path) {
			CheckMode();
			id = GetFullID(id, true);
			var tex = Texture2DLoader.LoadTexture2D(GetFullResourcePath(sprite_path));
			var mat = new Material(ShaderCache.Acquire(SpriteDefinition.DEFAULT_SHADER));
			mat.mainTexture = tex;
			var sprite_def = SpriteDefinition.Construct(mat, id);

			return sprite_def;
		}

		/// <summary>
		/// Registers an encounter icon (used for example in the Ammonomicon) from an image resource.
		/// </summary>
		/// <returns>The sprite definition of the new encounter icon.</returns>
		/// <param name="id">ID (including the mod's namespace) for the new encounter icon.</param>
		/// <param name="sprite_path">Relative resource path to the image.</param>
		public SpriteDefinition RegisterEncounterIcon(string id, string sprite_path) {
			CheckMode();
			id = GetFullID(id, true);

			var tex = Texture2DLoader.LoadTexture2D(GetFullResourcePath(sprite_path));
			var mat = new Material(ShaderCache.Acquire(SpriteDefinition.DEFAULT_SHADER));
			mat.mainTexture = tex;
			var sprite_def = SpriteDefinition.Construct(mat, id);

			SemiLoader.EncounterIconCollection.Register(sprite_def);

			return sprite_def;
		}

		/// <summary>
		/// Registers a new sprite collection.
		/// </summary>
		/// <returns>The new sprite collection.</returns>
		/// <param name="id">ID (including the mod's namespace) for the new collection.</param>
		/// <param name="defs">Optional array of sprite definitions to initialize the sprite collection with.</param>
		public SpriteCollection RegisterSpriteCollection(string id, params SpriteDefinition[] defs) {
			CheckMode();
			id = GetFullID(id, true);

			var coll = SpriteCollection.Construct(
				SemiLoader.SpriteCollectionStorageObject,
				id,
				id, // IDs are unique
				null, // TODO sprite collection is passed null material - look into this
				defs
			);

			Gungeon.SpriteCollections.Add(id, coll);

			return coll;
		}

		/// <summary>
		/// Registers a new sprite template.
		/// </summary>
		/// <returns>The new sprite template.</returns>
		/// <param name="id">ID (including the mod's namespace) for the new sprite template.</param>
		/// <param name="coll_id">Global ID of the sprite collection to uses.</param>
		/// <param name="start_def_id">Optional global ID of the sprite definition from the sprite collection to use, the first definition will be used if not specified.</param>
		public Sprite RegisterSpriteTemplate(string id, string coll_id, string start_def_id = null) {
			CheckMode();
			id = GetFullID(id, true);
			coll_id = GetFullID(coll_id, false);
			start_def_id = GetFullID(start_def_id, false);

			var coll = Gungeon.SpriteCollections[coll_id];

			if (start_def_id != null) {
				start_def_id = coll.SpritePool.ValidateEntry(start_def_id);
			}
			var starting_idx = 0;
			if (coll.SpriteDefinitions.Count == 0) throw new ArgumentException($"The collection must have at least one sprite definition");

			if (start_def_id != null) starting_idx = coll.GetIndex(start_def_id);
			if (starting_idx < 0) throw new ArgumentException($"Collection doesn't have a '{start_def_id}' definition.");

			var sprite = Sprite.Construct(
				SemiLoader.SpriteTemplateStorageObject,
				coll,
				starting_idx
			);

			Gungeon.SpriteTemplates.Add(id, sprite);

			return sprite;
		}



		/// <summary>
		/// Registers a new localization.
		/// </summary>
		/// <returns>The representation of the new localization.</returns>
		/// <param name="id">ID (including the mod's namespace) for the new localization.</param>
		/// <param name="path">Relative resource path to the localization text file.</param>
		/// <param name="lang_id">Global ID of the language to apply this localization for.</param>
		/// <param name="table">Target string table to apply this localization for.</param>
		/// <param name="allow_overwrite">If set to <c>true</c> allows this localization to overwrite others (note - depends on loading order!).</param>
		public I18N.ModLocalization RegisterLocalization(string id, string path, string lang_id, I18N.StringTable table, bool allow_overwrite = false) {
			CheckMode();
			id = GetFullID(id, true);
			lang_id = GetFullID(lang_id, false);

			lang_id = Gungeon.Languages.ValidateEntry(lang_id);

			var mod_loc = new I18N.ModLocalization(
				Info,
				GetFullResourcePath(path),
				lang_id,
				table,
				allow_overwrite
			);

			Gungeon.Localizations.Add(id, mod_loc);

			return mod_loc;
		}

		/// <summary>
		/// Registers a new PickupObject.
		/// </summary>
		/// <returns>Prefab object for the newly registered item.</returns>
		/// <param name="id">ID (including the mod's namespace) for the new item.</param>
		/// <param name="enc_icon_id">Global ID of the encounter icon to use for this item.</param>
		/// <param name="sprite_template_id">Global ID of the sprite template to use for this item.</param>
		/// <param name="name_key">Global ID of the localization string to use for the full name of this item.</param>
		/// <param name="short_desc_key">Global ID of the localization string to use for the short description of this item.</param>
		/// <param name="long_desc_key">Global ID of the localization string to use for the long description of this item.</param>
		/// <typeparam name="T">Component type to initialize as the PickupObject.</typeparam>
		public T RegisterItem<T>(string id, string enc_icon_id, string sprite_template_id, string name_key = "", string short_desc_key = "", string long_desc_key = "") where T : PickupObject {
			CheckMode();
			id = GetFullID(id, true);
			enc_icon_id = GetFullID(enc_icon_id, false);
			sprite_template_id = GetFullID(sprite_template_id, false);
			name_key = GetFullID(name_key, false);
			short_desc_key = GetFullID(short_desc_key, false);
			long_desc_key = GetFullID(long_desc_key, false);

			var sprite_def = SemiLoader.EncounterIconCollection.GetDefinition(enc_icon_id);
			if (sprite_def == null) throw new ArgumentException($"There is no sprite definition '{sprite_def}' in the encounter icon collection");
			var sprite_template = Gungeon.SpriteTemplates[sprite_template_id];

			var new_inst = PickupObjectTreeBuilder.GetNewInactiveObject(id);
			var pickup_object = PickupObjectTreeBuilder.AddPickupObject<T>(new_inst);
			((Patches.PickupObject)(object)pickup_object).UniqueItemID = id;
			if (pickup_object is Gun) {
				var gun = pickup_object as Gun;
				gun.Volley = ScriptableObject.CreateInstance<ProjectileVolleyData>();
				gun.Volley.projectiles = new List<ProjectileModule>();
				gun.Volley.projectiles.Add(new ProjectileModule());

				var barrel = PickupObjectTreeBuilder.GetNewBarrel();
				barrel.transform.parent = new_inst.transform;
				gun.barrelOffset = barrel.transform;
			}
			var journal_entry = PickupObjectTreeBuilder.CreateJournalEntry(name_key, long_desc_key, short_desc_key, sprite_def.Value.Name);
			var enc_track = PickupObjectTreeBuilder.AddEncounterTrackable(new_inst, journal_entry, $"SEMI/Items/{typeof(T).Name}/{id}");
			var enc_db_entry = PickupObjectTreeBuilder.CreateEncounterDatabaseEntry(enc_track, $"SEMI/Items/{id}");
			PickupObjectTreeBuilder.AddSprite(new_inst, sprite_template);

			PickupObjectDatabase.Instance.Objects.Add(pickup_object);
			Gungeon.Items.Add(id, pickup_object);
			var id_tag = Gungeon.ItemTag.Unknown;
			var t_type = typeof(T);
			if (t_type.IsAssignableFrom(typeof(Gun)) || t_type.IsAssignableFrom(typeof(PassiveItem)) || t_type.IsAssignableFrom(typeof(PlayerItem))) {
				id_tag = Gungeon.ItemTag.Item;
			} else {
				id_tag = Gungeon.ItemTag.Consumable;
			}
			Gungeon.Items.SetTag(id, id_tag);
			EncounterDatabase.Instance.Entries.Add(enc_db_entry);

			return pickup_object;
		}

		/// <summary>
		/// Registers a new synergy.
		/// </summary>
		/// <returns>Newly registered synergy entry.</returns>
		/// <param name="id">ID (including the mod's namespace) for the new item.</param>
		/// <param name="name_loc_id">ID of the localization key for the name of this synergy (StringTable.Synergies).</param>
		/// <param name="objects_required">How many of the listed items and guns are needed to trigger the synergy.</param>
		/// <param name="optional_gun_ids">A list of gun IDs that will count for the synergy, but aren't required to trigger it. DOES NOT SUPPORT CONTEXT IDS.</param>
		/// <param name="mandatory_gun_ids">A list of gun IDs that are required for the synergy to trigger. DOES NOT SUPPORT CONTEXT IDS.</param>
		/// <param name="optional_item_ids">A list of item IDs that will count for the synergy, but aren't required to trigger it. DOES NOT SUPPORT CONTEXT IDS.</param>
		/// <param name="mandatory_item_ids">A list of item IDs that are required for the synergy to trigger. DOES NOT SUPPORT CONTEXT IDS.</param>
		/// <param name="stat_modifiers">A list of stat modifiers to go into effect when the synergy is active.</param>
		/// <param name="active_when_gun_unequipped">Whether the synergy should be active if conditions are met, but none of the mentioned guns are being held.</param>
		/// <param name="ignore_lich_eye_bullets">Whether Lich's Eye Bullets should not attempt to activate this synergy when one of the listed guns is picked up.</param>
		/// <param name="require_at_least_one_gun_and_one_item">Whether the synergy should require both an item and a gun to be picked up to activate the synergy, regardless of whether <code>objects_required</code> is met.</param>
		/// <param name="suppress_vfx">Whether the synergy should avoid triggering the arrow visual effect.</param>
		/// <param name="on_activated">Delegate executed when the synergy becomes active.</param>
		/// <param name="on_deactivated">Delegate executed when the synergy stops being active.</param>
		public AdvancedSynergyEntry RegisterSynergy(string id, string name_loc_id, int objects_required, string[] optional_gun_ids = null, string[] mandatory_gun_ids = null, string[] optional_item_ids = null, string[] mandatory_item_ids = null, List<StatModifier> stat_modifiers = null, bool active_when_gun_unequipped = false, bool ignore_lich_eye_bullets = false, bool require_at_least_one_gun_and_one_item = false, bool suppress_vfx = false, Gungeon.SynergyStateChangeAction on_activated = null, Gungeon.SynergyStateChangeAction on_deactivated = null) {
			CheckMode();
			id = GetFullID(id, true);
			name_loc_id = GetFullID(name_loc_id, false);

			var syn = new Patches.AdvancedSynergyEntry {
				ActivationStatus = SynergyEntry.SynergyActivation.ACTIVE,
				OptionalGuns = IDPool<PickupObject>.ResolveList(optional_gun_ids),
				MandatoryGuns = IDPool<PickupObject>.ResolveList(mandatory_gun_ids),
				OptionalItems = IDPool<PickupObject>.ResolveList(optional_item_ids),
				MandatoryItems = IDPool<PickupObject>.ResolveList(mandatory_item_ids),
				NameKey = name_loc_id,
				statModifiers = stat_modifiers,
				NumberObjectsRequired = objects_required,
				ActiveWhenGunUnequipped = active_when_gun_unequipped,
				IgnoreLichEyeBullets = ignore_lich_eye_bullets,
				RequiresAtLeastOneGunAndOneItem = require_at_least_one_gun_and_one_item,
				SuppressVFX = suppress_vfx,
				bonusSynergies = new List<CustomSynergyType>(),
				UniqueID = id
			};

			//TODO @performance use a working state on this, like collections
			var synergies = new List<AdvancedSynergyEntry>(GameManager.Instance.SynergyManager.synergies);
			synergies.Add(syn);
			GameManager.Instance.SynergyManager.synergies = synergies.ToArray();

			if (on_activated != null) Gungeon.OnSynergyActivated(id, on_activated);
			if (on_deactivated != null) Gungeon.OnSynergyDeactivated(id, on_activated);

			return syn;
		}

		/// <summary>
		/// Loads a sprite collection in Semi Collection format.
		/// </summary>
		/// <returns>The newly registered sprite collection.</returns>
		/// <param name="path">Relative resource path to the file in Semi Collection format.</param>
		public SpriteCollection LoadSpriteCollection(string path) {
			CheckMode();
			path = GetFullResourcePath(path);
			var parsed = Tk0dConfigParser.ParseCollection(File.ReadAllText(path), Config.ID);
			var dir = Path.GetDirectoryName(path);
			return SpriteCollection.Load(parsed, dir, Config.ID);
		}

		/// <summary>
		/// Loads a sprite animation in Semi Animation format.
		/// </summary>
		/// <returns>The newly registered sprite animation.</returns>
		/// <param name="path">Relative resource path to the file in Semi Animation format.</param>
		public SpriteAnimation LoadSpriteAnimation(string path) {
			CheckMode();
			path = GetFullResourcePath(path);
			var parsed = Tk0dConfigParser.ParseAnimation(File.ReadAllText(path), Config.ID);
			var dir = Path.GetDirectoryName(path);
			return SpriteAnimation.Load(parsed, Config.ID);
		}

		public Sound RegisterSound(string id, string path) {
			CheckMode();
			id = GetFullID(id, true);
			path = GetFullResourcePath(path);
			var sound = new Sound(RayAudio.Sound.Load(path));
			Gungeon.ModAudioTracks.Add(id, sound);
			return sound;
		}

		public Music RegisterMusic(string id, string path) {
			CheckMode();
			id = GetFullID(id, true);
			path = GetFullResourcePath(path);
			var music = new Music(RayAudio.MusicStream.Load(path));
			Gungeon.ModAudioTracks.Add(id, music);
			return music;
		}

		public AudioEvent RegisterAudioEvent(string id, AudioEvent ev) {
			CheckMode();
			id = GetFullID(id, true);
			Gungeon.AudioEvents.Add(id, ev);
			return ev;
		}

		public AudioEvent AddAudioEventConsequence(string target_ev, string consequence_ev) {
			CheckMode();
			target_ev = GetFullID(target_ev, false);
			consequence_ev = GetFullID(consequence_ev, false);
			var consequence = Gungeon.AudioEvents[consequence_ev];
			Gungeon.AudioEvents[target_ev].AddConsequence(consequence);
			return consequence;
		}
	}
}
