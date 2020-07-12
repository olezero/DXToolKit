#region File description

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EntitySystem.cs" company="GAMADU.COM">
//     Copyright � 2013 GAMADU.COM. All rights reserved.
//
//     Redistribution and use in source and binary forms, with or without modification, are
//     permitted provided that the following conditions are met:
//
//        1. Redistributions of source code must retain the above copyright notice, this list of
//           conditions and the following disclaimer.
//
//        2. Redistributions in binary form must reproduce the above copyright notice, this list
//           of conditions and the following disclaimer in the documentation and/or other materials
//           provided with the distribution.
//
//     THIS SOFTWARE IS PROVIDED BY GAMADU.COM 'AS IS' AND ANY EXPRESS OR IMPLIED
//     WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
//     FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL GAMADU.COM OR
//     CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
//     CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
//     SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
//     ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
//     NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
//     ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
//     The views and conclusions contained in the software and documentation are those of the
//     authors and should not be interpreted as representing official policies, either expressed
//     or implied, of GAMADU.COM.
// </copyright>
// <summary>
//   Base of all Entity Systems. Provide basic functionalities.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

#endregion File description

namespace DXToolKit.ECS.System {
	#region Using statements

	using global::System;
	using global::System.Collections.Generic;
	using global::System.Diagnostics;
#if XBOX || WINDOWS_PHONE || PORTABLE || FORCEINT32
	using BigInteger = global::System.Int32;
#endif
#if !XBOX && !WINDOWS_PHONE && !PORTABLE
	using global::System.Numerics;
#endif
	using Blackboard;

	#endregion Using statements

	/// <summary><para>Base of all Entity Systems.</para>
	/// <para>Provide basic functionalities.</para></summary>
	public abstract class EntitySystem {
		/// <summary>The entity world.</summary>
		protected EntityWorld entityWorld;

		/// <summary>The actives.</summary>
		private IDictionary<int, Entity> actives;

		/// <summary>Initializes static members of the <see cref="EntitySystem"/> class.</summary>
		static EntitySystem() {
			BlackBoard = new BlackBoard();
		}

		/// <summary>Initializes a new instance of the <see cref="EntitySystem" /> class.</summary>
		protected EntitySystem() {
			SystemBit = 0;
			Aspect = Aspect.Empty();
			IsEnabled = true;
			Types = null;
		}

		/// <summary>Initializes a new instance of the <see cref="EntitySystem" /> class.</summary>
		/// <param name="types">The types.</param>
		protected EntitySystem(params Type[] types)
			: this() {
			Debug.Assert(types != null, "Types must not be null.");
			Debug.Assert(types.Length != 0, "Types must not be zero lengthed.");
			Aspect = Aspect.All(types);
			Types = types;
		}

		/// <summary>Initializes a new instance of the <see cref="EntitySystem"/> class.</summary>
		/// <param name="aspect">The aspect.</param>
		protected EntitySystem(Aspect aspect)
			: this() {
			Debug.Assert(aspect != null, "Aspect must not be null.");
			Aspect = aspect;
		}

		/// <summary>Gets or sets the black board.</summary>
		/// <value>The black board.</value>
		public static BlackBoard BlackBoard { get; protected set; }

		/// <summary>Gets all active Entities for this system.</summary>
		public IEnumerable<Entity> ActiveEntities {
			get { return actives.Values; }
		}

		/// <summary>Gets or sets the entity world.</summary>
		/// <value>The entity world.</value>
		public EntityWorld EntityWorld {
			get { return entityWorld; }

			protected internal set {
				entityWorld = value;
#if !XBOX && !WINDOWS_PHONE && !PORTABLE
				if (EntityWorld.IsSortedEntities) {
					actives = new SortedDictionary<int, Entity>();
				} else {
					actives = new Dictionary<int, Entity>();
				}
#else
                this.actives = new Dictionary<int, Entity>();
#endif
			}
		}

		/// <summary>Gets or sets a value indicating whether this instance is enabled.</summary>
		/// <value><see langword="true" /> if this instance is enabled; otherwise, <see langword="false" />.</value>
		public bool IsEnabled { get; set; }

		/// <summary>Gets or sets the system bit. (Setter only).</summary>
		/// <value>The system bit.</value>
		internal BigInteger SystemBit { private get; set; }

		/// <summary>Gets or sets the aspect.</summary>
		/// <value>The aspect.</value>
		protected Aspect Aspect { get; set; }

		/// <summary>Gets the types.</summary>
		/// <value>The types.</value>
		protected Type[] Types { get; private set; }

		/// <summary>Gets the merged types.</summary>
		/// <param name="requiredType">Type of the required.</param>
		/// <param name="otherTypes">The other types.</param>
		/// <returns>All specified types in an array.</returns>
		public static Type[] GetMergedTypes(Type requiredType, params Type[] otherTypes) {
			Debug.Assert(requiredType != null, "RequiredType must not be null.");

			Type[] types = new Type[1 + otherTypes.Length];
			types[0] = requiredType;
			for (int index = otherTypes.Length - 1; index >= 0; --index) {
				types[index + 1] = otherTypes[index];
			}

			return types;
		}

		/// <summary>Override to implement code that gets executed when systems are initialized.</summary>
		public virtual void LoadContent() { }

		/// <summary>Override to implement code that gets executed when systems are terminated.</summary>
		public virtual void UnloadContent() { }

		/// <summary>Called when [added].</summary>
		/// <param name="entity">The entity.</param>
		public virtual void OnAdded(Entity entity) { }

		/// <summary>Called when [change].</summary>
		/// <param name="entity">The entity.</param>
		public virtual void OnChange(Entity entity) {
			Debug.Assert(entity != null, "Entity must not be null.");

			bool contains = (SystemBit & entity.SystemBits) == SystemBit;
			////bool interest = (this.typeFlags & entity.TypeBits) == this.typeFlags;
			bool interest = Aspect.Interests(entity);

			if (interest && !contains) {
				Add(entity);
			} else if (!interest && contains) {
				Remove(entity);
			} else if (interest && contains && entity.IsEnabled) {
				Enable(entity);
			} else if (interest && contains && !entity.IsEnabled) {
				Disable(entity);
			}
		}

		/// <summary>Called when [disabled].</summary>
		/// <param name="entity">The entity.</param>
		public virtual void OnDisabled(Entity entity) { }

		/// <summary>Called when [enabled].</summary>
		/// <param name="entity">The entity.</param>
		public virtual void OnEnabled(Entity entity) { }

		/// <summary>Called when [removed].</summary>
		/// <param name="entity">The entity.</param>
		public virtual void OnRemoved(Entity entity) { }

		/// <summary>Processes this instance.</summary>
		public virtual void Process() {
			if (CheckProcessing()) {
				Begin();
				ProcessEntities(actives);
				End();
			}
		}

		/// <summary>Toggles this instance.</summary>
		public void Toggle() {
			IsEnabled = !IsEnabled;
		}

		/// <summary>Adds the specified entity.</summary>
		/// <param name="entity">The entity.</param>
		protected void Add(Entity entity) {
			Debug.Assert(entity != null, "Entity must not be null.");

			entity.AddSystemBit(SystemBit);
			if (entity.IsEnabled) {
				Enable(entity);
			}

			OnAdded(entity);
		}

		/// <summary>Begins this instance processing.</summary>
		protected virtual void Begin() { }

		/// <summary>Checks the processing.</summary>
		/// <returns><see langword="true" /> if this instance is enabled, <see langword="false" /> otherwise</returns>
		protected virtual bool CheckProcessing() {
			return IsEnabled;
		}

		/// <summary>Ends this instance processing.</summary>
		protected virtual void End() { }

		/// <summary>Interests in the specified entity.</summary>
		/// <param name="entity">The entity.</param>
		/// <returns><see langword="true" /> if any interests in entity, <see langword="false" /> otherwise</returns>
		protected virtual bool Interests(Entity entity) {
			return Aspect.Interests(entity);
		}

		/// <summary>Processes the entities.</summary>
		/// <param name="entities">The entities.</param>
		protected virtual void ProcessEntities(IDictionary<int, Entity> entities) { }

		/// <summary>Removes the specified entity.</summary>
		/// <param name="entity">The entity.</param>
		protected void Remove(Entity entity) {
			Debug.Assert(entity != null, "Entity must not be null.");

			entity.RemoveSystemBit(SystemBit);
			if (entity.IsEnabled) {
				Disable(entity);
			}

			OnRemoved(entity);
		}

		/// <summary>Disables the specified entity.</summary>
		/// <param name="entity">The entity.</param>
		private void Disable(Entity entity) {
			Debug.Assert(entity != null, "Entity must not be null.");

			if (!actives.ContainsKey(entity.Id)) {
				return;
			}

			actives.Remove(entity.Id);
			OnDisabled(entity);
		}

		/// <summary>Enables the specified entity.</summary>
		/// <param name="entity">The entity.</param>
		private void Enable(Entity entity) {
			Debug.Assert(entity != null, "Entity must not be null.");

			if (actives.ContainsKey(entity.Id)) {
				return;
			}

			actives.Add(entity.Id, entity);
			OnEnabled(entity);
		}
	}
}