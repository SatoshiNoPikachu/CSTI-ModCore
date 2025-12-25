# 模组核心（Mod Core）- CSTI MOD



## 简介

模组核心（Mod Core）是一个旨在简化模组开发的API框架，提供了数据加载器、额外数据存储器、本地化、UI、实用工具等常用功能。



当前版本：3.0.1

By.サトシの皮卡丘



## 安装说明

请将模组压缩包解压于 BepInEx\plugins 文件夹下。



## 更新日志

### Version 3.0.1

修复了模组服务初始化在输出日志时会出错的问题。



### Version 3.0.0 [重要更新]

游戏在 v1.05ae 版本中将 Unity 引擎升级至了 Unity 2022，故本次更新主要围绕新引擎与新版本展开，因此不再支持在 Unity 2019 环境中运行（游戏版本 v1.05ad 及以下）。

1. Unity引擎相关：
   - 目标框架由 net48 升级至 netstandard2.1，并利用新特性优化了部分代码（如异步I/O，字典操作等）以提高性能。
   - 引用的 Unity 库升级至相同版本。
   - 移除了键值对类型的解构扩展方法，因为在 netstandard2.1 该类型已自带实现。
2. 实现加载屏幕与游戏新增的加载屏幕的兼容，现在将优先显示模组核心的加载屏幕，在加载完成后再显示游戏的加载屏幕。
3. 游戏实用工具变更：
   - 移除了 Games.Util 类，原有的扩展块转移至对应的 CardExtensions 以及 StatExtensions 中。
   - 新增扩展属性 GameStat.InGame，用于获取游戏中对应状态的游戏实例。
   - 新增扩展属性 GameStat.Storage，用于获取对应状态实例的额外数据存储器。
   - 新增扩展属性 InGameStat.Storage，用于获取状态实例的额外数据存储器。
4. 新增委托扩展，提供 Action 以及其7个泛型版本的安全调用扩展方法。
5. 新增 Game 类，提供销毁事件，以及 GameManager 和 GraphicsManager 的实例访问。
6. BaseUnityPlugin 泛型基类新增字符串常量 ModCoreVersion，用于依赖模组表示构建时模组核心版本。
7. 移除了额外数据存储控制器，其在销毁时所负责的数据清理已转交给 Game 类。
8. 加载器现在会注册音频对象了。
9. 本地化文本的加载由固定第二列修改为文本所在行的最后一列。
10. LitJSON 库更新为 netstandard2.1 构建版本。



### Version 2.0.2

1. UI预制件的ID格式改动，原ActionButton的UID已失效。
2. 新增实验性UI预制件LogPopup以及配套的OptionButton与OptionButtonGroup。
3. 新增模块额外数据存储器（ExtraDataStorage），提供存储器、数据与代理和解析器，实现了卡牌和状态的额外数据功能。
4. 新增实用工具，提供以下扩展属性或方法：
   - GameStat.InGameValue，用于获取游戏中该状态的当前值。
   - InGameCardBase.Storage，用于获取卡牌的额外数据存储器。
   - InGameCardBase.GetDurabilityValue(DurabilitiesTypes)，用于根据耐久类型获取对应当前值。
5. Loader.LoadBeforeEvent事件的触发时机调整至初始化之后。



### Version 2.0.1

Database类根据命名空间与数据键获取数据的方法逻辑与返回值类型修改，并且新增了对应的泛型版本。



### Version 2.0.0 [重要更新]

1. 本次更新为不兼容更新，所有直接依赖的旧版本插件类型模组都需要更新才能正常工作。
2. 新增加载屏幕，用于在游戏启动时提供画面、进度以及错误提示。
3. 新增ModData类，用于描述模组的信息，包含命名空间和根目录属性。
4. 新增ModService类，用于模组信息的管理与提供服务（仅支持ModMeta.json格式），支持多路径加载（用于支持Steam创意工坊）。
5. 对加载器进行大幅修改：
   - 现在加载过程支持利用多线程提高加载速度；
   - 自动注册游戏程序集中所有派生自ScriptableObject的类型，这意味着现在将支持自动加载这些类型的数据；
   - 将根据模组信息进行加载，不再支持旧数据组织格式，数据键与Unity对象名称（name字段）将带有命名空间并支持检索；
   - 现在支持自动加载GameSourceModify数据；
   - 新增支持通过类型注册名以及数据键检索对象进行修改的DataObjectModify数据（文件内容格式仍旧是GameSourceModify）。
6. 与UnityAPI相关的扩展方法现已分离出独立类，这意味着使用这些方法的插件需重新编译。
7. 新增特性模组命名空间（ModNamespaceAttribute），BaseUnityPlugin泛型基类新增模组命名空间、模组数据属性，将特性应用在派生类上时，这些属性将自动根据该特性赋值。
8. 数据信息类新增属性允许回退到模组根目录加载（CanFallbackToRoot）以及相关构造方法，用于标记在模组Data目录下不存在对应数据文件夹时，加载器是否会回退到根目录尝试加载。



### Version 1.2.0

1. 加载器现在支持来自ModEditor的GameSourceModify的数据格式。
2. 新增类DataMap，提供了通过CardTag/CardTypes类型实例获取所对应的CardData的扩展方法。
3. 加载器现在支持递归加载子文件夹中的数据以及纹理。



### Version 1.1.3

修复了Loader对数组元素为原版结构体类型的加载错误问题。



### Version 1.1.2

1. 修复了Loader对列表元素为原版类型的加载错误问题。
2. 优化了Loader对数组元素为原版类型的加载性能。



### Version 1.1.1

通过Loader.Preload方法加载的UniqueIDScriptable类型对象注册将延迟至FixData阶段，以解决在部分情况下GameLoad还未被实例化的问题。



### Version 1.1.0

1. 加载器新增三个数据加载方法，并且现在支持原版精灵贴图的引用，但纹理加载方法不再公开。
2. 数据库新增添加对象方法。
3. 修复了ActionButton预制件的点击事件未被清空的问题。