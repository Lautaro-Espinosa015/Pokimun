using UnityEngine;
using UnityEngine.Windows.Speech;
using System.Collections.Generic;
using System.Linq;  


public class NewMonoBehaviourScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    KeywordRecognizer keywordRecognizer;
    Dictionary<string, System.Action> keywords = new Dictionary<string, System.Action>();

    void Start()
    {
        keywords.Add("terremoto", () =>
        {
            Debug.Log("Modo Chileno");
        });
        keywords.Add("macaco", () =>
        {
            Debug.Log("Modo Brasilero");
        });
        keywordRecognizer = new KeywordRecognizer(keywords.Keys.ToArray());

        keywordRecognizer.OnPhraseRecognized += KeywordRecognizer_OnPharseRecognized;

        keywordRecognizer.Start();

    }

    private void KeywordRecognizer_OnPharseRecognized(PhraseRecognizedEventArgs args)
    {
        Debug.Log($"Reconocido: '{args.text}' (confianza: {args.confidence})");
        System.Action keywordAction;

        if (keywords.TryGetValue(args.text, out keywordAction))
        { 
            keywordAction.Invoke();
        }
    
    }




    // Update is called once per frame
    void Update()
    {
        
    }
}
