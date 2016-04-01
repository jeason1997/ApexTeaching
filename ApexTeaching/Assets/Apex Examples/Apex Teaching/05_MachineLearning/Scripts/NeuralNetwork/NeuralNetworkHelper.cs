﻿namespace Apex.AI.NeuralNetwork
{
    using System;
    using System.IO;
    using Apex.Serialization;
    using UnityEngine;

    public static class NeuralNetworkHelper
    {
        public static void SaveNetwork(NeuralNetwork network, string filePath)
        {
            var now = DateTime.Now.ToString("dd-MM-yy_HH-mm-ss");
            var path = Path.Combine(filePath, now);
            if (File.Exists(path))
            {
                path = string.Concat(path, " (2)");
            }

            path = string.Concat(path, ".json");
            using (var file = File.CreateText(path))
            {
                file.Write(SerializationMaster.Serialize(network));
            }
        }

        public static NeuralNetwork LoadNetwork(TextAsset textAsset)
        {
            return SerializationMaster.Deserialize<NeuralNetwork>(textAsset.text);
        }
    }
}