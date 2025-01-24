# 模组核心（Mod Core）- CSTI MOD



## 简介

模组核心（Mod Core）是一个旨在简化模组开发的API框架，提供了数据加载器、本地化、UI等常用功能。



当前版本：1.2.0

By.サトシの皮卡丘



## 安装说明

请将Mod压缩包解压于 BepInEx\plugins 文件夹下。



## 更新日志

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