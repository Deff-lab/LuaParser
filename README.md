# LuaParser
C# class for parsing lua data

## Basic Usage
subscribe to the 'ParseEvent' event to notify about the completion of parsing of one data table
```
LuaParser.ParseEvent += YourHandlingFunction
```
and after call 'Parse' function, like this:
```
LuaParser.ParseAsync("{{ element = 0 }}");
```
after catching 'ParseEvent' you will get a **LuaValue** object where the values are stored in the 'Values' list if it is simple data like int, string, array 
**or**
'Childrens' list, where store data about children, for example after call this:
```
LuaParser.ParseAsync("{{ someInt = 0, someStr = \"Dog\", array = { 1, 2 }, structArray = {{1, \"cat\"}, {7, 'pasta'} }, child = { name = 'Piter', family = 'Guys' } }}");
```
you get **LuaValue** with architecture:
- luaValue
  - childrens
    - someInt
      - value: 0
    - someStr
      - value: "Dog"
    - array
      - values: 1, 2
    - structArray
      - childrens
        - element
          - values: 1, "cat"
        - element
          - values: 7, 'pasta'
    - child
      - childrens
        - name
          - value: 'Piter'
        - family
          - value: 'Guys'

## Dependence
.net framework 4.7.2 **or** you can just copy **LuaParser.cs**
