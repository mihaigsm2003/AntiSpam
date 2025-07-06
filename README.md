This plugin blocks the possibility of giving the same command multiple times within a set period of time.

The config is automatically created and can add any command by example:
```
{
  "CooldownSeconds": 10, // time after can use next command
  "ProtectedCommands": [
    "css_heal",
    "css_give",
    "css_vip",
    "css_say"
  ],
  "ConfigVersion": 1
}
```
