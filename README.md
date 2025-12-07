<!--
 * --------------------------------------------------------------------------------
 * Copyright (c) 2025 Vanishing Games. All Rights Reserved.
 * @Author: VanishXiao
 * @Date: 2025-10-30 16:25:44
 * @LastEditTime: 2025-12-07 20:21:35
 * --------------------------------------------------------------------------------
-->
> 《不尬的诗》
> 
> 自信满满写首诗
> 
> 坐到电脑前
> 
> 不出一刻钟
> 
> 写不出来
> 
> 糊弄一下提交上去
> 
> 还挺爽

# 资产命名规范

## 命名格式: 

**type_category_?subcategory_?action_?subcategory_001**

## 示例

```
ui_button_select
ui_button_shop_select
gp_proj_fire_hit_small_001
gp_proj_fire_hit_small_002
gp_booster_bomb_activate
mus_core_jungle_001
```

## 格式

- 使用snake_case
- 使用关键词大写来突出信息,如: mus_factory_main_STOP
- 使用camelCase来表示一个物体,如: enemy_fireDemon_death

## 要求

- 使用英文
- 简明扼要
- 层层嵌套: 按照从概括到具体的原则逐层嵌套
- 合理排序: 方便按照字母顺序合理且高效地对名称进行排序
- 统一数位: xxx 如 001
- 使用动词形式: bomb_activation vs. bomb_activate
- 使用正常时态: chest_destroyed vs. chest_destroy
- 保持单复数一致
- 使用游戏主题来命名: 不要使用机制来命名

## Tips

- 名称不要过长
- 适当使用描述词如 `loop` 表明音乐为循环
- 缩写必须在表中有
- 同一物体,团队用词要统一

## 缩写表

| 缩写      | 全称                 |
| ------- | ----------------------- |
| gp | gameplay         |
| plr    | player                 |
| char    | character |
| amb    | ambience                |
| mus      | music  |

## 反面例子

```
clip_01 # 没有上下文，不明所以
awesome_sound1 # 数字前没加下划线
boss_enemy_eggman # enemy 比 boss 更宽泛；应改用 enemy_boss_eggman
GreatArt_1 GreatArt_2 GreatArt_10 # 数位不一致
sfx_env_forest_daytime_birds_chirping_loop_ambient_lowIntensity_01.wav # 太长
```