using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuaParser
{
    public class LuaValue
    {
        /// <summary>
        /// Ключ значения
        /// </summary>
        public string key;

        #region Values - Значения

        /// <summary>
        /// Данные
        /// </summary>
        List<string> values;

        public List<string> Values
        {
            get
            {
                if (values == null)
                    values = new List<string>();

                return values;
            }
        }

        public void AddValue(string value)
        {
            Values.Add(value);
        }

        #endregion

        #region Children - Наследники

        List<LuaValue> children;

        List<LuaValue> Children
        {
            get
            {
                if (children == null)
                    children = new List<LuaValue>();

                return children;
            }
        }

        public void AddChildren(LuaValue children)
        {
            Children.Add(children);

            children.parent = this;
        }

        /// <summary>
        /// дочернее значение по ключу
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public LuaValue Child(string key) => Children.FirstOrDefault(e => e.key == key);

        public List<LuaValue> AllChildren => new List<LuaValue>(Children);

        #endregion

        /// <summary>
        /// Родительское значение
        /// </summary>
        public LuaValue parent;

        #region Get data - получение информаций 

        /// <summary>
        /// Получить значение
        /// </summary>
        /// <typeparam name="T">тип значения</typeparam>
        /// <param name="key">ключ</param>
        /// <returns>значение</returns>
        public T Get<T>(string key)
        {
            LuaValue value = Child(key);

            if (value == null)
                return default;

            return (T)GetFromValue<T>(value.Values[0]);
        }

        object GetFromValue<T>(string value)
        {
            switch (Type.GetTypeCode(typeof(T)))
            {
                case TypeCode.Int32:
                    return !string.IsNullOrEmpty(value) ? Convert.ToInt32(value) : (int)0;
                case TypeCode.Single:
                    return !string.IsNullOrEmpty(value) ? Convert.ToSingle(value.Replace('.', ',')) : (float)0.0f;
                case TypeCode.String:
                    return !string.IsNullOrEmpty(value) ? value.Trim(new char[2] { '\'', '\"' }) : string.Empty;
                default:
                    throw new Exception($"Converting value of type '{Type.GetTypeCode(typeof(T))}' not implemented");
            }
        }

        /// <summary>
        /// Подучить массив значений
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public T[] GetArray<T>(string key)
        {
            var value = Child(key);

            return value.Values.Select(e => (T)GetFromValue<T>(e)).ToArray();
        }

        #endregion

        #region Lua functions - Lua функций

        Dictionary<Guid, string> functions;

        public Dictionary<Guid, string> Functions
        {
            get
            {
                if (functions == null)
                    functions = new Dictionary<Guid, string>();

                return functions;
            }
        }

        public void GetMyFunctions(Dictionary<Guid, string> functions)
        {
            Values.ForEach(e =>
            {
                functions.ToList().ForEach(f =>
                {
                    if (e.Contains(f.Key.ToString().Replace("-", "_")))
                        Functions.Add(f.Key, f.Value);
                });
            }
            );

            foreach (var child in Children)
            {
                child.GetMyFunctions(functions);
            }
        }

        #endregion
    }
}
