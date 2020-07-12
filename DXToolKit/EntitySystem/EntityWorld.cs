#region File description

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EntityWorld.cs" company="GAMADU.COM">
//     Copyright � 2013 GAMADU.COM. All rights reserved.
//     Redistribution and use in source and binary forms, with or without modification, are
//     permitted provided that the following conditions are met:
//        1. Redistributions of source code must retain the above copyright notice, this list of
//           conditions and the following disclaimer.
//        2. Redistributions in binary form must reproduce the above copyright notice, this list
//           of conditions and the following disclaimer in the documentation and/or other materials
//           provided with the distribution.
//     THIS SOFTWARE IS PROVIDED BY GAMADU.COM 'AS IS' AND ANY EXPRESS OR IMPLIED
//     WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
//     FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL GAMADU.COM OR
//     CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
//     CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
//     SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
//     ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
//     NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
//     ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//     The views and conclusions contained in the software and documentation are those of the
//     authors and should not be interpreted as representing official policies, either expressed
//     or implied, of GAMADU.COM.
// </copyright>
// <summary>
//   The Entity World Class. Main interface of the Entity System.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

#endregion File description

namespace DXToolKit.ECS {
	#region Using statements

	using global::System;
	using global::System.Collections.Generic;
	using global::System.Diagnostics;
	using global::System.Linq;
	using global::System.Reflection;
	using Exceptions;
	using Interface;
	using Manager;
	using Utils;

	#endregion

	/// <summary><para>The Entity World Class.</para>
	/// <para>Main interface of the Entity System.</para></summary>
	public sealed class EntityWorld {
		/// <summary>The deleted.</summary>
		private readonly Bag<Entity> deleted;

		/// <summary>The entity templates.</summary>
		private readonly Dictionary<string, IEntityTemplate> entityTemplates;

		/// <summary>The pools.</summary>
		private readonly Dictionary<Type, IComponentPool<ComponentPoolable>> pools;

		/// <summary>The refreshed.</summary>
#if XBOX || WINDOWS_PHONE || PORTABLE
   		private readonly Bag<Entity> refreshed;
#else
		private readonly HashSet<Entity> refreshed;
#endif

		/// <summary>The date time.</summary>
		private DateTime dateTime;

		/// <summary>The pool cleanup delay counter.</summary>
		private int poolCleanupDelayCounter;

		/// <summary>
		/// If this instance is initialized
		/// </summary>
		private bool isInitialized = false;

#if !XBOX && !WINDOWS_PHONE
		/// <summary>
		/// Initializes a new instance of the <see cref="EntityWorld" /> class.
		/// </summary>
		/// <param name="isSortedEntities">if set to <c>true</c> [is sorted entities].</param>
		/// <param name="processAttributes">if set to <c>true</c> [process attributes].</param>
		/// <param name="initializeAll">if set to <c>true</c> [initialize all]. If you pass true here, there will be no need to call EntityWorld.InitializeAll() method</param>
		public EntityWorld(bool isSortedEntities = false, bool processAttributes = true, bool initializeAll = false) {
			IsSortedEntities = isSortedEntities;
#else
        /// <summary>Initializes a new instance of the <see cref="EntityWorld"/> class.</summary>
        public EntityWorld()
        {
            this.IsSortedEntities = false;
#endif
#if XBOX || WINDOWS_PHONE || PORTABLE
   		    this.refreshed = new Bag<Entity>();
#else
			refreshed = new HashSet<Entity>();
#endif
			pools = new Dictionary<Type, IComponentPool<ComponentPoolable>>();
			entityTemplates = new Dictionary<string, IEntityTemplate>();
			deleted = new Bag<Entity>();
			EntityManager = new EntityManager(this);
			SystemManager = new SystemManager(this);
			TagManager = new TagManager();
			GroupManager = new GroupManager();
			PoolCleanupDelay = 10;
			dateTime = FastDateTime.Now;
			if (initializeAll)
				InitializeAll(processAttributes);
		}

		/// <summary>Gets the current state of the entity world.</summary>
		/// <value>The state of the current.</value>
		public Dictionary<Entity, Bag<IComponent>> CurrentState {
			get {
				Bag<Entity> entities = EntityManager.ActiveEntities;
				Dictionary<Entity, Bag<IComponent>> currentState = new Dictionary<Entity, Bag<IComponent>>();
				for (int index = 0, j = entities.Count; index < j; ++index) {
					Entity entity = entities.Get(index);
					Bag<IComponent> components = entity.Components;
					currentState.Add(entity, components);
				}

				return currentState;
			}
		}

		/// <summary>Gets the delta time since last game loop in ticks.</summary>
		/// <value>The delta in ticks.</value>
		public long Delta { get; private set; }

		/// <summary>Gets the entity manager.</summary>
		/// <value>The entity manager.</value>
		public EntityManager EntityManager { get; private set; }

		/// <summary>Gets the group manager.</summary>
		/// <value>The group manager.</value>
		public GroupManager GroupManager { get; private set; }

		/// <summary>Gets or sets the interval in FrameUpdates between pools cleanup. Default is 10.</summary>
		/// <value>The pool cleanup delay.</value>
		public int PoolCleanupDelay { get; set; }

		/// <summary>Gets the system manager.</summary>
		/// <value>The system manager.</value>
		public SystemManager SystemManager { get; private set; }

		/// <summary>Gets the tag manager.</summary>
		/// <value>The tag manager.</value>
		public TagManager TagManager { get; private set; }

		/// <summary>Gets a value indicating whether this instance is sorted entities.</summary>
		/// <value><see langword="true" /> if this instance is sorted entities; otherwise, <see langword="false" />.</value>
		internal bool IsSortedEntities { get; private set; }

		/// <summary>Clears this instance.</summary>
		public void Clear() {
			foreach (Entity activeEntity in EntityManager.ActiveEntities.Where(activeEntity => activeEntity != null)) {
				activeEntity.Delete();
			}

			Update();
		}

		/// <summary>Creates the entity.</summary>
		/// <param name="entityUniqueId">The desired unique id of this Entity. if null, <c>artemis</c> will create an unique ID.
		/// This value can be accessed by using the property uniqueID of the Entity</param>
		/// <returns>A new entity.</returns>
		public Entity CreateEntity(long? entityUniqueId = null) {
			return EntityManager.Create(entityUniqueId);
		}

		/// <summary>Creates a entity from template.</summary>
		/// <param name="entityTemplateTag">The entity template tag.</param>
		/// <param name="templateArgs">The template arguments.</param>
		/// <returns>The created entity.</returns>
		/// <exception cref="MissingEntityTemplateException">EntityTemplate for the tag "entityTemplateTag" was not registered.</exception>
		public Entity CreateEntityFromTemplate(string entityTemplateTag, params object[] templateArgs) {
			return CreateEntityFromTemplate(null, entityTemplateTag, templateArgs);
		}

		/// <summary>Creates a entity from template.</summary>
		/// <param name="entityUniqueId">The entity unique id. (<c>artemis</c> can provide this value, use the overloaded method)</param>
		/// <param name="entityTemplateTag">The entity template tag.</param>
		/// <param name="templateArgs">The template arguments.</param>
		/// <returns>The created entity.</returns>
		/// <exception cref="MissingEntityTemplateException">EntityTemplate for the tag "entityTemplateTag" was not registered.</exception>
		public Entity CreateEntityFromTemplate(long entityUniqueId, string entityTemplateTag, params object[] templateArgs) {
			return CreateEntityFromTemplate((long?) entityUniqueId, entityTemplateTag, templateArgs);
		}

		/// <summary>Deletes the entity.</summary>
		/// <param name="entity">The entity.</param>
		public void DeleteEntity(Entity entity) {
			Debug.Assert(entity != null, "Entity must not be null.");

			deleted.Add(entity);
		}

		/// <summary>Gets a component from a pool.</summary>
		/// <param name="type">The type of the object to get.</param>
		/// <returns>The found component.</returns>
		/// <exception cref="Exception">There is no pool for the specified type</exception>
		public IComponent GetComponentFromPool(Type type) {
			Debug.Assert(type != null, "Type must not be null.");

			if (!pools.ContainsKey(type)) {
				throw new Exception("There is no pool for the specified type " + type);
			}

			return pools[type].New();
		}

		/// <summary>Gets the component from pool.</summary>
		/// <typeparam name="T">Type of the component</typeparam>
		/// <returns>The found component.</returns>
		/// <exception cref="Exception">There is no pool for the type  + type</exception>
		public T GetComponentFromPool<T>() where T : ComponentPoolable {
			return (T) GetComponentFromPool(typeof(T));
		}

		/// <summary>Gets the entity.</summary>
		/// <param name="entityId">The entity id.</param>
		/// <returns>The specified entity.</returns>
		public Entity GetEntity(int entityId) {
			Debug.Assert(entityId >= 0, "Id must be at least 0.");

			return EntityManager.GetEntity(entityId);
		}

		/// <summary>Gets the pool for a Type.</summary>
		/// <param name="type">The type.</param>
		/// <returns>The specified ComponentPool{ComponentPool-able}.</returns>
		public IComponentPool<ComponentPoolable> GetPool(Type type) {
			Debug.Assert(type != null, "Type must not be null.");

			return pools[type];
		}

		/// <summary>Initialize the EntityWorld.</summary>
		/// <param name="assembliesToScan">The assemblies to scan for data attributes.</param>
		public void InitializeAll(params Assembly[] assembliesToScan) {
			if (!isInitialized) {
				bool processAttributes = assembliesToScan != null && assembliesToScan.Length > 0;
				SystemManager.InitializeAll(processAttributes, assembliesToScan);
				isInitialized = true;
			}
		}

		/// <summary>Initialize the EntityWorld.
		/// Call this if you dont pass true in the parameter called InitializedALL in entity world constructor
		/// </summary>
		/// <param name="processAttributes">if set to <see langword="true" /> [process attributes].</param>
		public void InitializeAll(bool processAttributes = false) {
			if (!isInitialized) {
				SystemManager.InitializeAll(processAttributes);
				isInitialized = true;
			}
		}

		/// <summary>Loads the state of the entity.</summary>
		/// <param name="templateTag">The template tag. Can be null.</param>
		/// <param name="groupName">Name of the group. Can be null.</param>
		/// <param name="components">The components.</param>
		/// <param name="templateArgs">Parameters for entity template.</param>
		/// <returns>The <see cref="Entity" />.</returns>
		public Entity LoadEntityState(string templateTag, string groupName, IEnumerable<IComponent> components, params object[] templateArgs) {
			Debug.Assert(components != null, "Components must not be null.");

			Entity entity;
			if (!string.IsNullOrEmpty(templateTag)) {
				entity = CreateEntityFromTemplate(templateTag, -1, templateArgs);
			} else {
				entity = CreateEntity();
			}

			if (string.IsNullOrEmpty(groupName)) {
				GroupManager.Set(groupName, entity);
			}

			foreach (IComponent comp in components) {
				entity.AddComponent(comp);
			}

			return entity;
		}

		/// <summary>Sets the entity template.</summary>
		/// <param name="entityTag">The entity tag.</param>
		/// <param name="entityTemplate">The entity template.</param>
		public void SetEntityTemplate(string entityTag, IEntityTemplate entityTemplate) {
			entityTemplates.Add(entityTag, entityTemplate);
		}

		/// <summary>Sets the pool for a specific type</summary>
		/// <param name="type">The type.</param>
		/// <param name="pool">The pool.</param>
		public void SetPool(Type type, IComponentPool<ComponentPoolable> pool) {
			Debug.Assert(type != null, "Type must not be null.");
			Debug.Assert(pool != null, "Component pool must not be null.");

			pools.Add(type, pool);
		}

		/// <summary>Updates the EntityWorld.</summary>
		public void Update() {
			long deltaTicks = (FastDateTime.Now - dateTime).Ticks;
			dateTime = FastDateTime.Now;
			Update(deltaTicks);
		}

		/// <summary>Updates the EntityWorld.</summary>
		/// <param name="deltaTicks">The delta ticks.</param>
		public void Update(long deltaTicks) {
			Delta = deltaTicks;

			++poolCleanupDelayCounter;
			if (poolCleanupDelayCounter > PoolCleanupDelay) {
				poolCleanupDelayCounter = 0;
				foreach (Type item in pools.Keys) {
					pools[item].CleanUp();
				}
			}

			if (!deleted.IsEmpty) {
				for (int index = deleted.Count - 1; index >= 0; --index) {
					Entity entity = deleted.Get(index);
					TagManager.Unregister(entity);
					GroupManager.Remove(entity);
					EntityManager.Remove(entity);
					entity.DeletingState = false;
				}

				deleted.Clear();
			}

#if XBOX || WINDOWS_PHONE || PORTABLE
		    bool isRefreshing = !this.refreshed.IsEmpty;
#else
			bool isRefreshing = refreshed.Count > 0;
#endif
			if (isRefreshing) {
#if XBOX || WINDOWS_PHONE || PORTABLE
                for (int index = this.refreshed.Count - 1; index >= 0; --index)
                {
			    	Entity entity = this.refreshed.Get(index);
                    this.EntityManager.Refresh(entity);
                    entity.RefreshingState = false;
                }
#else
				foreach (Entity entity in refreshed) {
					EntityManager.Refresh(entity);
					entity.RefreshingState = false;
				}

#endif
				refreshed.Clear();
			}

			SystemManager.Update();
		}

		/// <summary>Draws the EntityWorld.</summary>
		public void Draw() {
			SystemManager.Draw();
		}

		/// <summary>Unloads the content.</summary>
		public void UnloadContent() {
			SystemManager.TerminateAll();
		}

		/// <summary>Refreshes the entity.</summary>
		/// <param name="entity">The entity.</param>
		internal void RefreshEntity(Entity entity) {
			Debug.Assert(entity != null, "Entity must not be null.");
#if XBOX || WINDOWS_PHONE || PORTABLE
			if(!this.refreshed.Contains(entity))
            {
				this.refreshed.Add(entity);
			}
#else
			refreshed.Add(entity);
#endif
		}

		/// <summary>Creates the entity from template.</summary>
		/// <param name="entityUniqueId">The entity unique id.</param>
		/// <param name="entityTemplateTag">The entity template tag.</param>
		/// <param name="templateArgs">The template arguments.</param>
		/// <returns>The Entity.</returns>
		/// <exception cref="MissingEntityTemplateException">Template for entity is missing.</exception>
		private Entity CreateEntityFromTemplate(long? entityUniqueId, string entityTemplateTag, params object[] templateArgs) {
			Debug.Assert(!string.IsNullOrEmpty(entityTemplateTag), "Entity template tag must not be null or empty.");

			Entity entity = EntityManager.Create(entityUniqueId);
			IEntityTemplate entityTemplate;
			entityTemplates.TryGetValue(entityTemplateTag, out entityTemplate);
			if (entityTemplate == null) {
				throw new MissingEntityTemplateException(entityTemplateTag);
			}

			entity = entityTemplate.BuildEntity(entity, this, templateArgs);
			RefreshEntity(entity);
			return entity;
		}
	}
}