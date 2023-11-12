using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using Random = UnityEngine.Random;

public class RandomUtil
{
    static Random.State m_random = Random.state;
    static Random.State previousRandom;

    private static Random.State State
    {
        get
        {
            return m_random = Random.state;
        }
    }

    public static float RandomValue
    {
        get
        {
            var value = Random.value;
            m_random = Random.state;
            return value;
        }
    }

    public static void ResetRandom()
    {
        m_random = previousRandom;
        Random.state = m_random;
    }

    public static void SetRandom(Random.State random)
    {
        previousRandom = m_random;
        m_random = random;
        Random.state = m_random;
    }

    public static byte[] SerializeRandom()
    {
        return SerializeRandom(State);
    }

    public static byte[] SerializeRandom(Random.State random)
    {
        /*
         * Legacy code
         * Type 'System.Random' is not marked as serializable.
         * 
        var binaryFormatter = new BinaryFormatter();
        using (var temp = new MemoryStream())
        {
            binaryFormatter.Serialize(temp, random);
            return temp.ToArray();
        }
        */

        var binaryFormatter = new BinaryFormatter();
        using var memoryStream = new MemoryStream();
        binaryFormatter.Serialize(memoryStream, random);
        return memoryStream.ToArray();
    }

    public static Random.State DeserializeRandom(byte[] rngData)
    {
        /* 
         * Legacy code
         * Type 'System.Random' is not marked as serializable.
         * 
        var binaryFormatter = new BinaryFormatter();
        using (var temp = new MemoryStream(rngData))
        {
            return (Random)binaryFormatter.Deserialize(temp);
        }
        */

        try
        {
            var binaryFormatter = new BinaryFormatter();
            using var memoryStream = new MemoryStream(rngData);
            return (Random.State)binaryFormatter.Deserialize(memoryStream);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);

            var previousState = Random.state;
            Random.InitState((int)DateTime.Now.Ticks);
            var tempState = Random.state;
            Random.state = previousState;
            return tempState;
        }
    }

    public static int GetRandomInt(int minimum, int maximum)
    {
        var value = Random.Range(minimum, maximum);
        m_random = Random.state;
        return value;
    }

    public static float GetRandomFloat(float minimum, float maximum)
    {
        var value = Random.Range(minimum, maximum);
        m_random = Random.state;
        return value;
    }

    public static T GetElementWeighted<T>(float rand, IEnumerable<T> enumerable, Func<T, float> weightFunc)
    {
        float sum = enumerable.Sum(e => weightFunc(e));
        float threshold = 0;

        foreach (var e in enumerable)
        {
            threshold += weightFunc(e) / sum;
            if (rand < threshold)
            {
                return e;
            }
        }

        return default(T);
    }

    public static T GetRandomElementWeighted<T>(IEnumerable<T> enumerable, Func<T, float> weightFunc)
    {
        float sum = enumerable.Sum(e => weightFunc(e));
        return GetRandomElementWeighted(enumerable, weightFunc, sum);
    }
    public static T GetRandomElementWeighted<T>(IEnumerable<T> enumerable, Func<T, float> weightFunc, float sum)
    {
        return GetRandomElementWeighted<T>(enumerable, weightFunc, sum, out int idx);
    }
    public static T GetRandomElementWeighted<T>(IEnumerable<T> enumerable, Func<T, float> weightFunc, float sum, out int idx)
    {
        var r = RandomValue;
        float threshold = 0;

        idx = -1;
        int i = 0;
        foreach (var e in enumerable)
        {
            threshold += weightFunc(e) / sum;
            if (r < threshold)
            {
                idx = i;
                return e;
            }
            i++;
        }

        return default(T);
    }

    public static T GetRandomElementWeighted<T>(Dictionary<T, float> weightDict)
    {
        return GetRandomElementWeighted(weightDict.Keys, (T t) => weightDict[t]);
    }

    public static T GetRandomElement<T>(IEnumerable<T> enumerable)
    {
        return GetRandomElement(enumerable, out var _);
    }

    public static T GetRandomElement<T>(IEnumerable<T> enumerable, out int idx)
    {
        var r = RandomValue;
        float threshold = 0;

        idx = -1;
        int i = 0;
        foreach (var value in enumerable)
        {
            threshold += 1f / enumerable.Count();
            if (r < threshold)
            {
                idx = i;
                return value;
            }
            i++;
        }

        return default(T);
    }

    public static List<T> GetRandomElements<T>(IEnumerable<T> enumerable, int count, bool replacement = false)
    {
        var pool = enumerable.ToList();
        var result = new List<T>();

        for (int i = 0; i < count; i++)
        {
            if (pool.Count == 0)
                break;
            var picked = GetRandomElement(pool, out var idx);
            result.Add(picked);
            if (!replacement)
                pool.RemoveAt(idx);

        }

        return result;
    }

    public static List<T> GetRandomElementsWeighted<T>(IEnumerable<T> enumerable, int count, Dictionary<T, float> weightDict, bool replacement = false)
    {
        var pool = enumerable.ToList();
        var result = new List<T>();
      

        for (int i = 0; i < count; i++)
        {
            if (pool.Count == 0)
                break;
            float sum = pool.Sum(t => weightDict[t]);
            var picked = GetRandomElementWeighted(pool,  (T t) => weightDict[t], sum,out var idx);
            result.Add(picked);
            if (!replacement)
                pool.RemoveAt(idx);

        }

        return result;
    }

    public static bool FlipCoin(double chance)
    {
        return RandomValue < chance;
    }

}