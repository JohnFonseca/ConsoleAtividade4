using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArquivoParaPrints : MonoBehaviour
{
    [SerializeField] [Tooltip("true = CPU, false = GPU")] bool CPU;
    [SerializeField] ComputeShader computeShader;
    // [SerializeField] float gravidade;
    int shaderPos, shaderVelPos, shaderCores;
    ComputeBuffer buffer, bufferTempo, bufferFinalizou;
    float[] valores;//, tempo;
    float[] tempo;
    int tempoPassado;
    [SerializeField] float massaMin, MassaMax;//, contMax;
                                              //  [SerializeField] RandomColor randomColor;
    bool testeIniciado;
    [SerializeField] GameObject colisao;
    // Start is called before the first frame update
    public GameObject modelPref;
    // public int counts = 100;
    public int interactions = 100;
    Cube[] data;
    GameObject[] objetos;

    int[] finalizou, corMudada;
    float Dt;
    float posInicial;
    [SerializeField] int quantidade;
    [SerializeField] float tempoFinal;

    private void inicializadorComum()
    {
        valores = new float[quantidade];
        tempo = new float[quantidade];
        finalizou = new int[quantidade];
        corMudada = new int[quantidade];
    }
    public void IniciarTeste()
    {
        /*                  print 1
        valores = new float[quantidade];
        buffer = new ComputeBuffer(quantidade, 8);
        shaderPos = computeShader.FindKernel("CSMain");
        buffer.SetData(valores);
        computeShader.SetFloat("VariavelValor", VariavelValor);
        computeShader.SetBuffer(shaderPos, "Result", buffer);
        computeShader.Dispatch(shaderPos, quantidade / 10, 1, 1);
         */
        buffer = new ComputeBuffer(quantidade, 8);
        bufferTempo = new ComputeBuffer(quantidade, 8);
        bufferFinalizou = new ComputeBuffer(quantidade, 8);

        shaderPos = computeShader.FindKernel("CSMain");
        shaderVelPos = computeShader.FindKernel("velShare");
        shaderCores = computeShader.FindKernel("Cores");
      
        buffer.SetData(valores);
        bufferFinalizou.SetData(finalizou);
      

        computeShader.SetFloat("massa", UnityEngine.Random.Range(massaMin, MassaMax));
        computeShader.SetFloat("Dt", tempoPassado);
        computeShader.SetBuffer(shaderPos, "Result", buffer);
        computeShader.SetBuffer(shaderVelPos, "tempo", bufferTempo);
        computeShader.SetBuffer(shaderPos, "finalizou", bufferFinalizou);
        // computeShader.SetBuffer(shaderCores,"cubes", data);
        computeShader.Dispatch(shaderPos, quantidade / 10, 1, 1);
        computeShader.SetFloat("PosColisao", colisao.transform.position.y);
        print(colisao.transform.position.y);
        buffer.GetData(valores);
        //print(buffer.count);
        //   buffer.Dispose();
        //  computeShader.SetBuffer(shaderCores, "cubes", data);
        this.transform.position = new Vector3(this.transform.position.x, -valores[0], this.transform.position.z);
        testeIniciado = true;
        // print(data.Length);
    }
    void Start()
    {
        //  Professor();
        inicializadorComum();
        createCubes();
        if (CPU == false)
            IniciarTeste();
        //   shaderVelPos = computeShader.FindKernel("velShare");
        //   computeShader.SetBuffer(shaderVelPos, "Posicoes", buffer);
    }
    private void Update()
    {
        if (CPU)
            CPUQueda();
        else
            ControleDeVelocidade();

    }

    private void ControleDeVelocidade()
    {


        bufferTempo.SetData(tempo);
        computeShader.Dispatch(shaderPos, quantidade / 10, 1, 1);
        computeShader.Dispatch(shaderVelPos, quantidade / 10, 1, 1);
        bufferTempo.GetData(tempo);
        buffer.GetData(valores);
        //  print(tempo[0]);
        computeShader.SetFloat("Dt", Time.deltaTime + tempo[0]);


        bufferFinalizou.GetData(finalizou);

        ProfessorGPU();

    }

    private void ProfessorGPU() //random gpu
    {
        int totalSize = sizeof(float) * 3 + sizeof(float) * 4;

        ComputeBuffer computeBuffer = new ComputeBuffer(data.Length, totalSize);
        computeBuffer.SetData(data);

        computeShader.SetBuffer(shaderCores, "cubes", computeBuffer);
        computeShader.SetInt("interactions", interactions);
        computeShader.Dispatch(shaderCores, data.Length / 10, 1, 1);

        computeBuffer.GetData(data);

        for (int i = 0; i < objetos.Length; i++)
        {
            if (corMudada[i] == 0)
                objetos[i].transform.position = new Vector3(objetos[i].transform.position.x, -valores[i], objetos[i].transform.position.z);
            if (finalizou[i] == 1 && corMudada[i] == 0)
            {
                objetos[i].GetComponent<MeshRenderer>().material.SetColor("_Color", data[i].color);
                corMudada[i] = 1;
                print(objetos[i].GetComponent<MeshRenderer>().material.color);
                tempoFinal = tempo[0];
            }

        }
        computeBuffer.Dispose();
    }

    private void CPUQueda()
    {
        float Posicao = 0;
        float gravidade = 9.8f; // vai receber 9.8
                                //  float forca; // = massa * aceleracao
        float velocidade;
        velocidade = gravidade * Dt;//(forca / massa); // gravidade * 10
        Posicao = -velocidade * Dt / 2; // + posicao inicial

        Dt += Time.deltaTime;
        //  print(colisao.transform.position.y);
        if (Posicao >= colisao.transform.position.y + 0.75)
        {
            //    print(Posicao + objetos[0].transform.position.y);
            for (int i = 0; i < objetos.Length; i++)
            {
                if (corMudada[i] == 0)
                    objetos[i].transform.position = new Vector3(objetos[i].transform.position.x, Posicao, objetos[i].transform.position.z);

            }
        }
        else
        {
            for (int i = 0; i < objetos.Length; i++)
            {
                if (corMudada[i] == 0)
                {
                    objetos[i].GetComponent<MeshRenderer>().material.SetColor("_Color", UnityEngine.Random.ColorHSV());
                    corMudada[i] = 1;
                    print(objetos[i].GetComponent<MeshRenderer>().material.color);
                    tempoFinal = Dt;
                }
            }
        }
        posInicial = objetos[0].transform.position.y;
    }
    void createCubes()
    {
        data = new Cube[quantidade];
        objetos = new GameObject[quantidade];

        for (int i = 0; i < quantidade; i++)
        {
            float offsetX = (-quantidade / 2 + i);

            //   for (int k = 0; k < counts; k++)
            //   {
            // float offsetY = (-counts / 2 + k);
            float offsetY = (-quantidade / 2);
            Color _colorInic = UnityEngine.Random.ColorHSV();

            GameObject go = GameObject.Instantiate(modelPref, new Vector3(offsetX * 0.6f, 0, 0), Quaternion.identity);
            go.GetComponent<MeshRenderer>().material.SetColor("_Color", _colorInic);
            // gameObjects[i * counts + k] = go;
            objetos[i] = go;
            data[i] = new Cube();
            data[i].position = go.transform.position;
            data[i].color = _colorInic;
        }
    }
    private void OnDestroy()
    {
        if (CPU == false)
        {
            buffer.Release();
            bufferTempo.Release();
            bufferFinalizou.Release();
        }
    }
}

/*[System.Serializable]
public struct Cube
{
    public Vector3 position;
    public Color color;
}
*/