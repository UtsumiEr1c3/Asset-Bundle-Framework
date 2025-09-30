using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using PlasticPipe.PlasticProtocol.Messages;
using UnityEngine;

public static class XmlUtility
{
    public static T Read<T>(string fileName) where T : class
    {
        FileStream stream = null;
        if (!File.Exists(fileName))
        {
            return default(T);
        }

        try
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            stream = File.OpenRead(fileName);
            XmlReader reader = XmlReader.Create(stream);
            T instance = (T)serializer.Deserialize(reader);
            stream.Close();
            return instance;
        }
        catch
        {
            if (stream != null)
            {
                stream.Close();
            }

            return default(T);
        }
    }
}
