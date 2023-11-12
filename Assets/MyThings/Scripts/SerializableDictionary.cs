using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public interface IUpdateSerializationPresentation
{
    void UpdateSerializationPresentation();
}
[Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver,IUpdateSerializationPresentation
{
    [SerializeField]
    protected List<TKey> keys = new List<TKey>();

    [SerializeField]
    protected List<TValue> values = new List<TValue>();

    bool isSerializing = false;

    protected List<TKey> tempKeys = new List<TKey>();

    // save the dictionary to lists
    public void OnBeforeSerialize()
    {

        //Debug.Log ("OnBeforeSerialize");
        if (isSerializing)
        {
            //Debug.Log ("OnBeforeSerializeHalted");
            return;
        }
        isSerializing = true;

        keys.Clear();
        values.Clear();


        foreach (KeyValuePair<TKey, TValue> pair in this)
        {
            if (this.ContainsKey(pair.Key))
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }
        keys.AddRange(tempKeys);

        //Debug.Log ("OnAfterBeforeSerialize");
        isSerializing = false;

    }

    // load dictionary from lists
    public virtual void OnAfterDeserialize()
    {

        this.Clear();
        tempKeys.Clear();

        //if (keys.Count != values.Count)
        //throw new System.Exception(string.Format("there are {0} keys and {1} values after deserialization. Make sure that both key and value types are serializable."));

        for (int i = 0; i < keys.Count; i++)
        {
            if(keys[i] == null){
                continue;
            }
            if(this.ContainsKey(keys[i]))
            {
                tempKeys.Add(keys[i]);
                continue;
            }

            if(i < values.Count){
                this.Add(keys[i], values[i]);
            }else{
                values.Add(default(TValue));
                this.Add(keys[i],values[i]);
            }
        }
        if(keys.Count < values.Count){
            values.RemoveRange(keys.Count, values.Count - keys.Count);
        }
    }

    public override string ToString()
    {
        string result = "";
        foreach (var kvp in this)
        {
            result += kvp.Key + ":" + kvp.Value;
            result += ",";
        }

        if (result.Length > 0)
            result = result.Remove(result.Length - 1);
        return result;
    }

    public void UpdateSerializationPresentation()
    {
        if (keys.Count < Keys.Count)
        {
            keys = Keys.ToList();
            values = Values.ToList();
        }
    }
}

public class DictionaryStringT<T> : SerializableDictionary<string, T>
{
    public override void OnAfterDeserialize()
    {
        
        this.Clear();

        while (keys.Count > values.Count)
        {
            values.Add(default(T));
        }


        for (int i = 0; i < keys.Count; i++)
        {
            while (string.IsNullOrEmpty(keys[i]) || this.ContainsKey(keys[i]))
            {
                keys[i] = "Element_" + i.ToString();
                if (this.ContainsKey(keys[i]))
                {
                    System.Random r = new System.Random();
                    keys[i] = r.Next(int.MaxValue).ToString();
                }

            }
            this.Add(keys[i], values[i]);
        }
    }
}

[Serializable]
public class DictionaryStringInt : DictionaryStringT<int>
{

}

[Serializable]
public class DictionaryStringString : DictionaryStringT<string>
{

}

[Serializable]
public class DictionaryStringFloat : DictionaryStringT<float>
{

}

[Serializable]
public class DictionaryStringGameObject : DictionaryStringT<GameObject>
{

}

[Serializable]
public class DictionaryIntString : SerializableDictionary<int,string>
{

}

[Serializable]
public class DictionaryIntFloat : SerializableDictionary<int,float>
{

}

[Serializable]
public class DictionaryIntGameObject: SerializableDictionary<int, GameObject>
{

}