using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;


[System.Serializable]
public class GoogleData
{
	public string order, result, msg, value;
}


public class GoogleSheetManager : MonoBehaviour
{
	public static GoogleSheetManager Instance { get; private set; }
	const string URL = "https://script.google.com/macros/s/AKfycbwhKBNPhn-ZdE7xplBRw444JfnIoj4pGHi1ByPr8rvi8pru6Kz9ws_ixuq7P90poh8b/exec";
	public GoogleData GD;
	public TMP_InputField IDInput, PassInput;
	string id, pass;
	[SerializeField] private TextMeshProUGUI errorMessageTMP;
	[SerializeField] private GameObject LoadingImage;
	
	private void Awake() {
		if(Instance == null) {
			Instance = this;
			DontDestroyOnLoad(gameObject);
		} else {
			Destroy(gameObject);
		}
	}

	bool SetIDPass()
	{
		id = IDInput.text.Trim();
		pass = PassInput.text.Trim();

		if (id == "" || pass == "") return false;
		else return true;
	}
	public void Register()
	{
		if (!SetIDPass())
		{
			print("아이디 또는 비밀번호가 비어있습니다");
			return;
		}

		WWWForm form = new WWWForm();
		form.AddField("order", "register");
		form.AddField("id", id);
		form.AddField("pass", pass);

		StartCoroutine(Post(form));
	}
	public void Login()
	{
		if (!SetIDPass())
		{
			print("아이디 또는 비밀번호가 비어있습니다");
			return;
		}

		WWWForm form = new WWWForm();
		form.AddField("order", "login");
		form.AddField("id", id);
		form.AddField("pass", pass);

		StartCoroutine(Post(form));
	}
	void OnApplicationQuit()
	{
		WWWForm form = new WWWForm();
		form.AddField("order", "logout");

		StartCoroutine(Post(form));
	}
	[ContextMenu("벨류 세팅오기")]
	public void SetValue()
	{
		WWWForm form = new WWWForm();
		form.AddField("order", "setValue");
		form.AddField("value", 1); // 값 변경.

		StartCoroutine(Post(form));
	}
	[ContextMenu("벨류 가져오기")]
	public void GetValue()
	{
		WWWForm form = new WWWForm();
		form.AddField("order", "getValue");

		StartCoroutine(Post(form));
	}
	IEnumerator Post(WWWForm form)
	{
		if (LoadingImage != null)
		{
			LoadingImage.gameObject.SetActive(true);
		}
		using (UnityWebRequest www = UnityWebRequest.Post(URL, form)) // 반드시 using을 써야한다
		{
			yield return www.SendWebRequest();

			if (www.isDone) Response(www.downloadHandler.text);
			else print("웹의 응답이 없습니다.");
		}
		if (LoadingImage != null)
		{
			LoadingImage.gameObject.SetActive(false);
		}
	}
	void Response(string json)
	{
		if (string.IsNullOrEmpty(json)) return;

		GD = JsonUtility.FromJson<GoogleData>(json);

		if (GD.result == "ERROR")
		{
			print(GD.order + "을 실행할 수 없습니다. 에러 메시지 : " + GD.msg);
			if (errorMessageTMP != null)
			{
				errorMessageTMP.color = Color.red;
				errorMessageTMP.text = GD.msg + ".";
			}
			return;
		}
		else
		{
			if (errorMessageTMP != null)
			{
				errorMessageTMP.color = Color.black;
				errorMessageTMP.text = "";
			}
		}

		print(GD.order + "을 실행했습니다. 메시지 : " + GD.msg);
		if (GD.msg == "로그인 완료")
		{
			SceneManager.LoadScene("TestScene_Doyoon");
		}
		if (errorMessageTMP != null)
		{
			errorMessageTMP.color = Color.black;
			errorMessageTMP.text = GD.msg + ".";
		}

		if (GD.order == "getValue")
		{
			print(GD.value);
			// = GD.value;
		}
	}
}