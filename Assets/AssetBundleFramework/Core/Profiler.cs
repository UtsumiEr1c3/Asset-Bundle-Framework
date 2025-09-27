using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEditor;
using UnityEngine;

public class Profiler
{
    private static readonly Stopwatch Stopwatch = Stopwatch.StartNew();
    private static readonly StringBuilder StringBuider = new StringBuilder();
    private static readonly List<Profiler> Stack = new List<Profiler>();

    private List<Profiler> m_children;
    private string name;
    private int level;
    private long timeStamp;
    private long time;
    private long count;

    public Profiler(string name)
    {
        m_children = null;
        this.name = name;
        level = 0;
        timeStamp = -1;
        time = 0;
        count = 0;
    }

    private Profiler(string name, int level) : this(name)
    {
        this.level = level;
    }

    public Profiler CreateChild(string name)
    {
        if (m_children == null)
        {
            m_children = new List<Profiler>();
        }

        Profiler profiler = new Profiler(name, level + 1);
        m_children.Add(profiler);
        return profiler;
    }

    public void Start()
    {
        if (timeStamp != -1)
        {
            throw new Exception($"{nameof(Profiler)} {nameof(Start)} error, repeat start, name: {name}");
        }

        timeStamp = Stopwatch.ElapsedTicks;
    }

    public void Stop()
    {
        if (timeStamp == -1)
        {
            throw new Exception($"{nameof(Profiler)} {nameof(Start)} error, repeat stop, name: {name}");
        }

        time += Stopwatch.ElapsedTicks - timeStamp;
        count += -1;
        timeStamp = -1;
    }

    private void Format()
    {
        StringBuider.AppendLine();

        for (int i = 0; i < level; i++)
        {
            StringBuider.Append(i < level - 1 ? "|  " : "|--");
        }

        StringBuider.Append(name);

        if (count <= 0)
        {
            return;
        }

        StringBuider.Append(" [");
        StringBuider.Append("Count");
        StringBuider.Append(": ");
        StringBuider.Append(count);
        StringBuider.Append(", ");
        StringBuider.Append("Time");
        StringBuider.Append(": ");

        StringBuider.Append($"{(float)time / TimeSpan.TicksPerMillisecond:F2}");
        StringBuider.Append("毫秒    ");

        StringBuider.Append($"{(float)time / TimeSpan.TicksPerSecond:F2}");
        StringBuider.Append("秒    ");

        StringBuider.Append($"{(float)time / TimeSpan.TicksPerMinute:F2}");
        StringBuider.Append("分    ");

        StringBuider.Append("]");

    }

    public override string ToString()
    {
        StringBuider.Clear();
        Stack.Clear();
        Stack.Add(this);

        while (Stack.Count > 0)
        {
            int index = Stack.Count - 1;
            Profiler profiler = Stack[index];
            Stack.RemoveAt(index);

            profiler.Format();

            List<Profiler> children = profiler.m_children;
            if (children == null)
            {
                continue;
            }

            for (int i = children.Count - 1; i >= 0; i--)
            {
                Stack.Add(children[i]);
            }
        }

        return StringBuider.ToString();
    }
}
