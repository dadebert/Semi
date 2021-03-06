﻿using System;
namespace Semi {
    public static class Registry {/// <summary>
		/// Delegate used for synergy activation/synergy deactivation events.
		/// </summary>
		public delegate void SynergyStateChangeAction(PlayerController p);

		/// <summary>
		/// ID pool containing all the items in the game, including consumables, guns, passives and actives.
		/// This pool contains all Gungeon and mod pickups, including unused/excluded/unobtainable ones.
		/// </summary>
		/// <value>ID pool of the items.</value>
        public static IDPool<PickupObject> Items { get; internal set; }
		/// <summary>
		/// ID pool containing all the enemies in the game. This includes companions, which operate in the same way as enemies.
		/// This pool contains all Gungeon and mod enemies, including unused/excluded/unobtainable ones.
		/// </summary>
		/// <value>ID pool of the enemies.</value>
        public static IDPool<AIActor> Enemies { get; internal set; }
		/// <summary>
		/// ID pool containing all the synergies in the game.
		/// This pool contains all Gungeon and mod synergies, including unused/excluded/unobtainable ones.
		/// </summary>
		/// <value>ID pool of the synergies.</value>
		public static IDPool<AdvancedSynergyEntry> Synergies { get; internal set; }

		/// <summary>
		/// ID pool containing all sprite collections registered by mods.
		/// At this moment, this ID pool does not contain any Gungeon sprite collections. This might change in the future.
		/// </summary>
		/// <value>ID pool of mod sprite collections.</value>
		public static IDPool<SpriteCollection> SpriteCollections { get; internal set; }
		/// <summary>
		/// ID pool containing all sprite templates registered by mods.
		/// At this moment, this ID pool does not contain any Gungeon sprites. This is unlikely to change in the future.
		/// </summary>
		/// <value>ID pool of mod sprite templates.</value>
		public static IDPool<Sprite> SpriteTemplates { get; internal set; }
		/// <summary>
		/// ID pool containing all animations registered by mods.
		/// At this moment, this ID pool does not contain any Gungeon animations. This is unlikely to change in the future.
		/// </summary>
		/// <value>ID pool of mod animations.</value>
		public static IDPool<SpriteAnimation> AnimationTemplates { get; internal set; }

		/// <summary>
		/// ID pool containing all localizations for all languages.
		/// This pool contains both Gungeon and modded localizations, for every string table in every builtin language.
		/// </summary>
		/// <value>ID pool of available localizations.</value>
		public static IDPool<I18N.LocalizationSource> Localizations { get; internal set; }

		/// <summary>
		/// ID pool containing all available languages.
		/// This pool contains both Gungeon and modded languages.
		/// </summary>
		/// <value>ID pool of available languages.</value>
		public static IDPool<I18N.Language> Languages { get; internal set; }

		/// <summary>
		/// ID pool containing audio tracks registered by mods.
		/// At this moment, this ID pool does not contain any Gungeon (WWise) audio. This will likely never change.
		/// </summary>
		/// <value>ID pool of mod sounds.</value>
		public static IDPool<Audio> ModAudioTracks { get; internal set; }

		/// <summary>
		/// ID pool containing audio events.
		/// This ID pool includes Gunegon (WWise) audio events.
		/// </summary>
		/// <value>ID pool of audio events.</value>
		public static IDPool<AudioEvent> AudioEvents { get; internal set; }

		public static IDPool<UI.MenuOption> MenuOptionTypes { get; internal set; }

		public static IDPool<UI.MenuOption> ModMenuOptions { get; internal set; }

		/// <summary>
		/// Registers a delegate to be ran when the synergy becomes active.
		/// </summary>
		/// <param name="id">ID of the synergy.</param>
		/// <param name="action">Action to invoke.</param>
		public static void OnSynergyActivated(ID id, SynergyStateChangeAction action) {
			SynergyStateChangeAction existing_action = null;
			if (!SemiLoader.SynergyActivatedActions.TryGetValue(id, out existing_action)) {
				SemiLoader.SynergyActivatedActions[id] = action;
			} else {
				existing_action += action;
			}
		}

		/// <summary>
		/// Registers a delegate to be ran when the synergy becomes inactive.
		/// </summary>
		/// <param name="id">ID of the synergy.</param>
		/// <param name="action">Action to invoke.</param>
		public static void OnSynergyDeactivated(ID id, SynergyStateChangeAction action) {
			SynergyStateChangeAction existing_action = null;
			if (!SemiLoader.SynergyDeactivatedActions.TryGetValue(id, out existing_action)) {
				SemiLoader.SynergyDeactivatedActions[id] = action;
			} else {
				existing_action += action;
			}
		}

		/// <summary>
		/// Checks whether a synergy is currently actiive.
		/// </summary>
		/// <returns><c>true</c>, if synergy is active, <c>false</c> otherwise.</returns>
		/// <param name="id">ID of the synergy.</param>
		public static bool IsSynergyActive(ID id) {
			return SemiLoader.ActiveSynergyIDs.Contains(id);
		}
    }
}
