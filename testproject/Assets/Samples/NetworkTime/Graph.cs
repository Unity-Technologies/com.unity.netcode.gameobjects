using System.Collections;
using System.Collections.Generic;
using Tayx.Graphy;
using Tayx.Graphy.Graph;
using UnityEngine;
using UnityEngine.UI;

public class Graph : G_Graph
{
    [SerializeField]
    private Image m_ImageGraph = null;

    // [SerializeField]
    // private Shader ShaderFull = null;
    // [SerializeField]
    // private Shader ShaderLight = null;

    // This keeps track of whether Init() has run or not
    [SerializeField]
    private bool m_IsInitialized = false;

    private int m_Resolution = 150;
    private float[] m_Values;

    private G_GraphShader m_ShaderGraph = null;

    public float Max { get; set; } = 0.4f;

    public float Min { get; set; } = 0.0f;

    private float m_Next;

    public void UpdateGraph(float value)
    {
        m_Next = value;
        UpdateGraph();
    }

    // private void FixedUpdate()
    // {
    //     UpdateGraph();
    // }

    protected override void UpdateGraph()
    {
        if (!m_IsInitialized)
        {
            Init();
        }

        if (m_ShaderGraph.ShaderArrayValues == null)
        {
            m_Values = new float[m_Resolution];
            m_ShaderGraph.ShaderArrayValues = new float[m_Resolution];
        }

        float currentMax = 0f;

        for (int i = 0; i <= m_Resolution - 1; i++)
        {
            if (i >= m_Resolution - 1)
            {
                m_Values[i] = m_Next;
            }
            else
            {
                m_Values[i] = m_Values[i + 1];
            }

            // Store the highest fps to use as the highest point in the graph

            if (currentMax < m_Values[i])
            {
                currentMax = m_Values[i];
            }
        }

        //Max = currentMax;

        for (int i = 0; i <= m_Resolution - 1; i++)
        {
            m_ShaderGraph.ShaderArrayValues[i] = ((m_Values[i] - Min) / (Max - Min));
        }

        m_ShaderGraph.UpdatePoints();
    }

    protected override void CreatePoints()
    {
        if (m_ShaderGraph.ShaderArrayValues == null || m_Values.Length != m_Resolution)
        {
            m_Values = new float[m_Resolution];
            m_ShaderGraph.ShaderArrayValues = new float[m_Resolution];
        }

        for (int i = 0; i < m_Resolution; i++)
        {
            m_ShaderGraph.ShaderArrayValues[i] = 0;
        }

        // m_shaderGraph.GoodColor = m_graphyManager.GoodFPSColor;
        // m_shaderGraph.CautionColor = m_graphyManager.CautionFPSColor;
        // m_shaderGraph.CriticalColor = m_graphyManager.CriticalFPSColor;

        m_ShaderGraph.UpdateColors();

        m_ShaderGraph.UpdateArray();
    }

    private void Init()
    {
        m_ShaderGraph = new G_GraphShader
        {
            Image = m_ImageGraph
        };

        UpdateParameters();

        m_IsInitialized = true;
    }

    public void UpdateParameters()
    {
        if (m_ShaderGraph == null)
        {
            // TODO: While Graphy is disabled (e.g. by default via Ctrl+H) and while in Editor after a Hot-Swap,
            // the OnApplicationFocus calls this while m_shaderGraph == null, throwing a NullReferenceException
            return;
        }

        m_ShaderGraph.ArrayMaxSize = G_GraphShader.ArrayMaxSizeFull;

        //m_shaderGraph.Image.material    = new Material(ShaderFull);

        m_ShaderGraph.InitializeShader();

        //m_Resolution = m_graphyManager.FpsGraphResolution;

        CreatePoints();
    }
}
