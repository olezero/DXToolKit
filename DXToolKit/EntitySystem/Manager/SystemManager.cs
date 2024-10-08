#region File description

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SystemManager.cs" company="GAMADU.COM">
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
//   Class SystemManager.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

#endregion File description

namespace DXToolKit.ECS.Manager {
	#region Using statements

	using global::System;
	using global::System.Collections;
	using global::System.Collections.Generic;
	using global::System.Linq;
	using global::System.Reflection;
#if FULLDOTNET || CLIENTPROFILE
	using global::System.Threading.Tasks;
#endif
	using Attributes;
	using Interface;
	using Utils;

#if (!FULLDOTNET && !PORTABLE && !METRO) && !CLIENTPROFILE
    using ParallelTasks;
#endif

#if METRO
    using Parallel = global::System.Threading.Tasks.Parallel;
#endif

	#endregion Using statements

	/// <summary>Class SystemManager.</summary>
	public sealed class SystemManager {
		/// <summary>The entity world.</summary>
		private readonly EntityWorld entityWorld;

		/// <summary>The systems.</summary>
		private readonly IDictionary<Type, IList> systems;

		/// <summary>The merged bag.</summary>
		private readonly Bag<System.EntitySystem> mergedBag;

		/// <summary>The update layers.</summary>
		private IDictionary<int, SystemLayer> updateLayers;

		/// <summary>The draw layers.</summary>
		private IDictionary<int, SystemLayer> drawLayers;

		/// <summary>Initializes a new instance of the <see cref="SystemManager" /> class.</summary>
		/// <param name="entityWorld">The entity world.</param>
		internal SystemManager(EntityWorld entityWorld) {
			mergedBag = new Bag<System.EntitySystem>();
#if FULLDOTNET
			drawLayers = new SortedDictionary<int, SystemLayer>();
			updateLayers = new SortedDictionary<int, SystemLayer>();
#else
            this.drawLayers = new Dictionary<int, SystemLayer>();
            this.updateLayers = new Dictionary<int, SystemLayer>();
#endif
			systems = new Dictionary<Type, IList>();
			this.entityWorld = entityWorld;
		}

		/// <summary>Gets the systems.</summary>
		/// <value>The systems.</value>
		public Bag<System.EntitySystem> Systems {
			get { return mergedBag; }
		}

		/// <summary>Sets the system.</summary>
		/// <typeparam name="T">The <see langword="Type" /> T.</typeparam>
		/// <param name="system">The system.</param>
		/// <param name="gameLoopType">Type of the game loop.</param>
		/// <param name="layer">The layer.</param>
		/// <param name="executionType">Type of the execution.</param>
		/// <returns>The set system.</returns>
		public T SetSystem<T>(T system, GameLoopType gameLoopType, int layer = 0, ExecutionType executionType = ExecutionType.Synchronous) where T : System.EntitySystem {
			return (T) SetSystem(system.GetType(), system, gameLoopType, layer, executionType);
		}

		/// <summary>
		/// Gets the systems.
		/// </summary>
		/// <typeparam name="T">The EntitySystem</typeparam>
		/// <returns>A List of System Instances</returns>
		public List<T> GetSystems<T>() where T : System.EntitySystem {
			IList system;

			systems.TryGetValue(typeof(T), out system);

			return (List<T>) system;
		}

		/// <summary>
		/// Gets the system.
		/// </summary>
		/// <typeparam name="T">The EntitySystem</typeparam>
		/// <returns>The system instance</returns>
		/// <exception cref="InvalidOperationException">There are more or none systems of the type passed</exception>
		public T GetSystem<T>() where T : System.EntitySystem {
			IList systems;

			this.systems.TryGetValue(typeof(T), out systems);

			if (systems != null && systems.Count > 1) {
				throw new InvalidOperationException(string.Format("System list contains more than one element of type {0}", typeof(T)));
			}

			return (T) systems[0];
		}

		/// <summary>Initializes all.</summary>
		/// <param name="processAttributes">if set to <see langword="true" /> [process attributes].</param>
		/// <param name="assembliesToScan">The assemblies to scan.</param>
		/// <exception cref="Exception">propertyComponentPool is null.</exception>
		internal void InitializeAll(bool processAttributes, IEnumerable<Assembly> assembliesToScan = null) {
			if (processAttributes) {
				IDictionary<Type, List<Attribute>> types;
				if (assembliesToScan == null) {
#if FULLDOTNET || METRO
					types = AttributesProcessor.Process(AttributesProcessor.SupportedAttributes);
#else
                    types = AttributesProcessor.Process(AttributesProcessor.SupportedAttributes, null);
#endif
				} else {
					types = AttributesProcessor.Process(AttributesProcessor.SupportedAttributes, assembliesToScan);
				}

				foreach (KeyValuePair<Type, List<Attribute>> item in types) {
#if METRO
                    if (typeof(EntitySystem).GetTypeInfo().IsAssignableFrom(item.Key.GetTypeInfo()))
#else
					if (typeof(System.EntitySystem).IsAssignableFrom(item.Key))
#endif
					{
						Type type = item.Key;
						Attributes.EntitySystemAttribute pee = (Attributes.EntitySystemAttribute) item.Value[0];
						System.EntitySystem instance = (System.EntitySystem) Activator.CreateInstance(type);
						SetSystem(instance, pee.GameLoopType, pee.Layer, pee.ExecutionType);
					}
#if METRO
                    else if (typeof(IEntityTemplate).GetTypeInfo().IsAssignableFrom(item.Key.GetTypeInfo()))
#else
					else if (typeof(IEntityTemplate).IsAssignableFrom(item.Key))
#endif
					{
						Type type = item.Key;
						EntityTemplateAttribute pee = (EntityTemplateAttribute) item.Value[0];
						IEntityTemplate instance = (IEntityTemplate) Activator.CreateInstance(type);
						entityWorld.SetEntityTemplate(pee.Name, instance);
					}
#if METRO
                    else if (typeof(ComponentPoolable).GetTypeInfo().IsAssignableFrom(item.Key.GetTypeInfo()))
#else
					else if (typeof(ComponentPoolable).IsAssignableFrom(item.Key))
#endif
					{
						CreatePool(item.Key, item.Value);
					}
				}
			}

			for (int index = 0, j = mergedBag.Count; index < j; ++index) {
				mergedBag.Get(index).LoadContent();
			}
		}

		/// <summary>Terminates all.</summary>
		internal void TerminateAll() {
			for (int index = 0; index < Systems.Count; ++index) {
				System.EntitySystem entitySystem = Systems.Get(index);
				entitySystem.UnloadContent();
			}

			Systems.Clear();
		}

		/// <summary>Updates the specified execution type.</summary>
		internal void Update() {
			Process(updateLayers);
		}

		/// <summary>Updates the specified execution type.</summary>
		internal void Draw() {
			Process(drawLayers);
		}

		/// <summary>Processes the specified systems to process.</summary>
		/// <param name="systemsToProcess">The systems to process.</param>
		private static void Process(IDictionary<int, SystemLayer> systemsToProcess) {
			foreach (int item in systemsToProcess.Keys) {
				if (systemsToProcess[item].Synchronous.Count > 0) {
					ProcessBagSynchronous(systemsToProcess[item].Synchronous);
				}
#if !PORTABLE
				if (systemsToProcess[item].Asynchronous.Count > 0) {
					ProcessBagAsynchronous(systemsToProcess[item].Asynchronous);
				}
#endif
			}
		}

		/// <summary>Sets the system.</summary>
		/// <param name="layers">The layers.</param>
		/// <param name="system">The system.</param>
		/// <param name="layer">The layer.</param>
		/// <param name="executionType">Type of the execution.</param>
		private static void SetSystem(ref IDictionary<int, SystemLayer> layers, System.EntitySystem system, int layer, ExecutionType executionType) {
			if (!layers.ContainsKey(layer)) {
				layers[layer] = new SystemLayer();
			}

			Bag<System.EntitySystem> updateBag = layers[layer][executionType];

			if (!updateBag.Contains(system)) {
				updateBag.Add(system);
			}
#if !FULLDOTNET
            layers = (from d in layers orderby d.Key ascending select d).ToDictionary(pair => pair.Key, pair => pair.Value);
#endif
		}

		/// <summary>Creates the instance.</summary>
		/// <param name="type">The type.</param>
		/// <returns>The specified ComponentPool-able instance.</returns>
		private static ComponentPoolable CreateInstance(Type type) {
			return (ComponentPoolable) Activator.CreateInstance(type);
		}

		/// <summary>Updates the bag synchronous.</summary>
		/// <param name="entitySystems">The entitySystems.</param>
		private static void ProcessBagSynchronous(Bag<System.EntitySystem> entitySystems) {
			for (int index = 0, j = entitySystems.Count; index < j; ++index) {
				entitySystems.Get(index).Process();
			}
		}

#if !PORTABLE
		/// <summary>Updates the bag asynchronous.</summary>
		/// <param name="entitySystems">The entity systems.</param>
		private static void ProcessBagAsynchronous(IEnumerable<System.EntitySystem> entitySystems) {
			Parallel.ForEach(entitySystems, entitySystem => entitySystem.Process());
			/*
			foreach (var system in entitySystems) {
				system.Process();
			}
			*/
		}
#endif

		/// <summary>Creates the pool.</summary>
		/// <param name="type">The type.</param>
		/// <param name="attributes">The attributes.</param>
		/// <exception cref="NullReferenceException">propertyComponentPool is null.</exception>
		private void CreatePool(Type type, IEnumerable<Attribute> attributes) {
			ComponentPoolAttribute propertyComponentPoolAttribute = null;

			foreach (ComponentPoolAttribute artemisComponentPool in attributes.OfType<ComponentPoolAttribute>()) {
				propertyComponentPoolAttribute = artemisComponentPool;
			}
#if METRO
            IEnumerable<MethodInfo> methods = type.GetRuntimeMethods();
#else
			MethodInfo[] methods = type.GetMethods();
#endif
			IEnumerable<MethodInfo> methodInfos = from methodInfo in methods
				let methodAttributes = methodInfo.GetCustomAttributes(false)
				from attribute in methodAttributes.OfType<ComponentCreateAttribute>()
				select methodInfo;

			Func<Type, ComponentPoolable> create = null;

			foreach (MethodInfo methodInfo in methodInfos) {
#if METRO
                create = (Func<Type, ComponentPoolable>) methodInfo.CreateDelegate(typeof(Func<Type, ComponentPoolable>));
#else
				create = (Func<Type, ComponentPoolable>) Delegate.CreateDelegate(typeof(Func<Type, ComponentPoolable>), methodInfo);
#endif
			}

			if (create == null) {
				create = CreateInstance;
			}

			IComponentPool<ComponentPoolable> pool;

			////Type[] typeArgs = { type };
			////Type d1 = typeof(ComponentPool<>);
			////Type typeGen = d1.MakeGenericType(typeArgs);
			////Activator.CreateInstance(typeGen, new object[] {PropertyComponentPool.InitialSize, PropertyComponentPool.IsResizable, create}

			if (propertyComponentPoolAttribute == null) {
				throw new NullReferenceException("propertyComponentPool is null.");
			}

			if (!propertyComponentPoolAttribute.IsSupportMultiThread) {
				pool = new ComponentPool<ComponentPoolable>(propertyComponentPoolAttribute.InitialSize, propertyComponentPoolAttribute.ResizeSize, propertyComponentPoolAttribute.IsResizable, create, type);
			} else {
				pool = new ComponentPoolMultiThread<ComponentPoolable>(propertyComponentPoolAttribute.InitialSize, propertyComponentPoolAttribute.ResizeSize, propertyComponentPoolAttribute.IsResizable, create, type);
			}

			entityWorld.SetPool(type, pool);
		}

		/// <summary>Sets the system.</summary>
		/// <param name="systemType">Type of the system.</param>
		/// <param name="system">The system.</param>
		/// <param name="gameLoopType">Type of the game loop.</param>
		/// <param name="layer">The layer.</param>
		/// <param name="executionType">Type of the execution.</param>
		/// <returns>The EntitySystem.</returns>
		private System.EntitySystem SetSystem(Type systemType, System.EntitySystem system, GameLoopType gameLoopType, int layer = 0, ExecutionType executionType = ExecutionType.Synchronous) {
			system.EntityWorld = entityWorld;

			if (systems.ContainsKey(systemType)) {
				systems[systemType].Add(system);
			} else {
				Type genericType = typeof(List<>);
				Type listType = genericType.MakeGenericType(systemType);
				systems[systemType] = (IList) Activator.CreateInstance(listType);
				systems[systemType].Add(system);
			}

			switch (gameLoopType) {
				case GameLoopType.Draw:
				{
					SetSystem(ref drawLayers, system, layer, executionType);
				}

					break;
				case GameLoopType.Update:
				{
					SetSystem(ref updateLayers, system, layer, executionType);
				}

					break;
			}

			if (!mergedBag.Contains(system)) {
				mergedBag.Add(system);
			}

			system.SystemBit = SystemBitManager.GetBitFor(system);

			return system;
		}

		/// <summary>The system layer class.</summary>
		private sealed class SystemLayer {
			/// <summary>The synchronous.</summary>
			public readonly Bag<System.EntitySystem> Synchronous;

			/// <summary>The asynchronous.</summary>
			public readonly Bag<System.EntitySystem> Asynchronous;

			/// <summary>Initializes a new instance of the <see cref="SystemLayer"/> class.</summary>
			public SystemLayer() {
				Asynchronous = new Bag<System.EntitySystem>();
				Synchronous = new Bag<System.EntitySystem>();
			}

			/// <summary>Gets the <see cref="Bag{EntitySystem}"/> with the specified execution type.</summary>
			/// <param name="executionType">Type of the execution.</param>
			/// <returns>The Bag{EntitySystem}.</returns>
			/// <exception cref="ArgumentOutOfRangeException">The ExecutionType must be Synchronous (or Asynchronous [if supported]).</exception>
			public Bag<System.EntitySystem> this[ExecutionType executionType] {
				get {
					switch (executionType) {
						case ExecutionType.Synchronous:
							return Synchronous;

#if !XBOX && !WINDOWS_PHONE && !PORTABLE
						case ExecutionType.Asynchronous:
							return Asynchronous;
#endif
						default:
							throw new ArgumentOutOfRangeException("executionType");
					}
				}
			}
		}
	}
}