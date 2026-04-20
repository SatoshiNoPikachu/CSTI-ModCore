# 模组核心（Mod Core）- CSTI MOD



## 简介

模组核心（Mod Core）是一个旨在简化模组开发的API框架，提供了数据加载器、额外数据存储器、本地化、UI、实用工具等常用功能。



当前版本：3.1.1

By.サトシの皮卡丘



## 安装说明

请将模组压缩包解压于 BepInEx\plugins 文件夹下。



## 更新日志

### Version 3.1.1

现在预加载场景时将会销毁场景内物体。



### Version 3.1.0

对额外数据模块进行了部分功能与 API 的重新设计。

- 额外数据存储器（ExtraDataStorage）：
  - 修改了原始数据形式的格式，现在数据键无符号限制了。
  - 新增方法 TryGetProxy，用于尝试获取代理。
  - 新增方法 TryGetValue，用于尝试获取值。
  - 方法 Get(string, string) 更名为 GetOrAdd。
  - 新增方法 GetOrAdd(string key, T def, IParser)，用于获取或添加代理。
  - 移除方法 Set(string, string)。
  - 移除方法 Set(string, T, IParser)。
- 额外数据 （ExtraData）：
  - 新增属性 Type，用于获取数据代理的目标数据类型。
  - 方法 GetProxy(IParse) 当已存在代理且目标类型不匹配时将抛出异常。
  - 新增方法 GetProxy(T, IParser)，用于获取或创建指定类型和值的数据代理。
  - 方法 Create(T, IParse) 现在不再将值转换为字符串，而是直接使用值创建代理，并且访问修饰符修改为 internal。
  - 新增方法 Create(T, out DataProxy{T}, IParser)，访问修饰符为 internal，用于创建额外数据对象及代理。
  - 移除方法 SetProxy。
  - 移除方法 TrySetData。
- 数据代理（DataProxy）：
  - 新增属性 Type，用于获取目标数据类型。
  - Value 现在是属性，而不是字段。
  - 方法 Create 转移至数据代理基类，访问修饰符修改为 internal。



### Version 3.0.3

- 对加载器进行了类分部操作。
- 加载屏幕类现在移入到加载器类中成为嵌套类。
- 优化加载器修改数组时数组拷贝的性能。
- 修复加载器修改数组/列表的添加模式中，未传递模组数据导致命名空间解析失效的问题。
- Game 类新增 PreInitOneShotEvent 事件。
- 加载器现在支持引用和修改在游戏场景中挂载而导致游戏启动时未加载的 ScriptableObject 类型对象。
  - 通过提前加载游戏场景实现，并在场景激活时会阻止 GameManager.Awake 方法以及其大部分补丁方法的执行。
  - GameManager.Awake 方法及其补丁方法的异常将被拦截并输出在日志中。
- 修复加载器获取映射对象时存在的空引用问题。
- 额外数据模块的更多方法支持自动获取默认解析器。
- 卡牌和状态额外数据的加载与保存等方法以及类现在是内部的。
- 修复额外数据存储器设置字符串数据且数据不存在代理时成功设置数据却返回 false 的问题。
- 修复加载器无法正确加载游戏结构体类型列表元素的问题。



### Version 3.0.2

- 加载器加载过程优化：合并数据加载、纹理加载和数据修复，并通过配置等待器将部分操作转移至后台线程，提高性能。
- 由于加载过程变化，加载屏幕的相关文本也进行相应改动。
- 加载器 UniqueIDScriptable 类型加载策略优化，该类对象的 Init() 方法执行将转移到数据修复完成后，以解决过早执行该方法但数据未完全加载完成而可能出现的问题。
- 修复加载器修改数组映射数据时，未传递模组数据导致命名空间解析失效的问题。
- Database.AddData(Type, IDictionary) 方法现在仅支持 Dictionary<string, T> 类型严格相同的数据字典。
- 加载器现在支持多态对象引用了，语法为：“TypeName|Namespace:ObjectName”。



### Version 3.0.1

修复了模组服务初始化在输出日志时会出错的问题。



### Version 3.0.0 [重要更新]

游戏在 v1.05ae 版本中将 Unity 引擎升级至了 Unity 2022，故本次更新主要围绕新引擎与新版本展开，因此不再支持在 Unity 2019 环境中运行（游戏版本 v1.05ad 及以下）。

- Unity引擎相关：
   - 目标框架由 net48 升级至 netstandard2.1，并利用新特性优化了部分代码（如异步I/O，字典操作等）以提高性能。
   - 引用的 Unity 库升级至相同版本。
   - 移除了键值对类型的解构扩展方法，因为在 netstandard2.1 该类型已自带实现。
- 实现加载屏幕与游戏新增的加载屏幕的兼容，现在将优先显示模组核心的加载屏幕，在加载完成后再显示游戏的加载屏幕。
- 游戏实用工具变更：
   - 移除了 Games.Util 类，原有的扩展块转移至对应的 CardExtensions 以及 StatExtensions 中。
   - 新增扩展属性 GameStat.InGame，用于获取游戏中对应状态的游戏实例。
   - 新增扩展属性 GameStat.Storage，用于获取对应状态实例的额外数据存储器。
   - 新增扩展属性 InGameStat.Storage，用于获取状态实例的额外数据存储器。
- 新增委托扩展，提供 Action 以及其7个泛型版本的安全调用扩展方法。
- 新增 Game 类，提供销毁事件，以及 GameManager 和 GraphicsManager 的实例访问。
- BaseUnityPlugin 泛型基类新增字符串常量 ModCoreVersion，用于依赖模组表示构建时模组核心版本。
- 移除了额外数据存储控制器，其在销毁时所负责的数据清理已转交给 Game 类。
- 加载器现在会注册音频对象了。
- 本地化文本的加载由固定第二列修改为文本所在行的最后一列。
- LitJSON 库更新为 netstandard2.1 构建版本。



### Version 2.0.2

- UI预制件的ID格式改动，原 ActionButton 的 UID 已失效。
- 新增实验性UI预制件 LogPopup 以及配套的 OptionButton 与 OptionButtonGroup。
- 新增模块额外数据存储器（ExtraDataStorage），提供存储器、数据与代理和解析器，实现了卡牌和状态的额外数据功能。
- 新增实用工具，提供以下扩展属性或方法：
   - GameStat.InGameValue，用于获取游戏中该状态的当前值。
   - InGameCardBase.Storage，用于获取卡牌的额外数据存储器。
   - InGameCardBase.GetDurabilityValue(DurabilitiesTypes)，用于根据耐久类型获取对应当前值。
- Loader.LoadBeforeEvent 事件的触发时机调整至初始化之后。



### Version 2.0.1

Database 类根据命名空间与数据键获取数据的方法逻辑与返回值类型修改，并且新增了对应的泛型版本。



### Version 2.0.0 [重要更新]

- 本次更新为不兼容更新，所有直接依赖的旧版本插件类型模组都需要更新才能正常工作。
- 新增加载屏幕，用于在游戏启动时提供画面、进度以及错误提示。
- 新增 ModData 类，用于描述模组的信息，包含命名空间和根目录属性。
- 新增 ModService 类，用于模组信息的管理与提供服务（仅支持 ModMeta.json 格式），支持多路径加载（用于支持 Steam 创意工坊）。
- 对加载器进行大幅修改：
   - 现在加载过程支持利用多线程提高加载速度；
   - 自动注册游戏程序集中所有派生自 ScriptableObject 的类型，这意味着现在将支持自动加载这些类型的数据；
   - 将根据模组信息进行加载，不再支持旧数据组织格式，数据键与 Unity 对象名称（name 字段）将带有命名空间并支持检索；
   - 现在支持自动加载 GameSourceModify 数据；
   - 新增支持通过类型注册名以及数据键检索对象进行修改的 DataObjectModify 数据（文件内容格式仍旧是 GameSourceModify）。
- 与 UnityAPI 相关的扩展方法现已分离出独立类，这意味着使用这些方法的插件需重新编译。
- 新增特性模组命名空间（ModNamespaceAttribute），BaseUnityPlugin 泛型基类新增模组命名空间、模组数据属性，将特性应用在派生类上时，这些属性将自动根据该特性赋值。
- 数据信息类新增属性允许回退到模组根目录加载（CanFallbackToRoot）以及相关构造方法，用于标记在模组 Data 目录下不存在对应数据文件夹时，加载器是否会回退到根目录尝试加载。



### Version 1.2.0

- 加载器现在支持来自 ModEditor 的 GameSourceModify 的数据格式。
- 新增类 DataMap，提供了通过 CardTag/CardTypes 类型实例获取所对应的CardData的扩展方法。
- 加载器现在支持递归加载子文件夹中的数据以及纹理。



### Version 1.1.3

修复了 Loader 对数组元素为原版结构体类型的加载错误问题。



### Version 1.1.2

- 修复了 Loader 对列表元素为原版类型的加载错误问题。
- 优化了 Loader 对数组元素为原版类型的加载性能。



### Version 1.1.1

通过 Loader.Preload 方法加载的 UniqueIDScriptable 类型对象注册将延迟至 FixData 阶段，以解决在部分情况下 GameLoad 还未被实例化的问题。



### Version 1.1.0

- 加载器新增三个数据加载方法，并且现在支持原版精灵贴图的引用，但纹理加载方法不再公开。
- 数据库新增添加对象方法。
- 修复了 ActionButton 预制件的点击事件未被清空的问题。