﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Firebase;
using Firebase.Database;
using Firebase.Unity.Editor;

namespace FBDebugger 
{
	/////-------------------------------------------------------------------------------------------------------/////
	///////////////////////////////<-----------Firebase Data Service----------->/////////////////////////////////////
	/////-------------------------------------------------------------------------------------------------------/////

	// Enums for sorting and filtering options
	public enum SortOption { None, Child, Key, Value }
	public enum FilterOption { None, LimitFirst, LimitLast, StartAt, EndAt, EqualTo }

	/// <summary>
	/// Data service class that encapsulates all functionality and interactions dealing with Google Firebase.
	///  - Singleton => uses DontDestroyOnLoad to keep Firebase instance alive through scene changes
	///  - Event driven => all data is emitted through events, other scripts are responsible for event subscriptions
	/// </summary>
	public class FBDataService : MonoBehaviour 
	{
		// Singleton instance 
		public static FBDataService instance = null;

		// Reference variables
		[HideInInspector]
		public string[] childNodes = new string[0];

		[HideInInspector]
		public SortOption sortBy = SortOption.None;

		[HideInInspector]
		public string sortValue = "";

		[HideInInspector]
		public FilterOption filterBy = FilterOption.None;

		[HideInInspector]
		public int filterValue = 0;
			
		// Property list variables
		public UnityEngine.Object plistObject;
		public static Hashtable plist 
		{
			get
			{
				// Parses GoogleService-Info.plist file if possible.
				// Script Asset => http://www.chrisdanielson.com/2011/05/09/using-apple-property-list-files-with-unity3d/
				Hashtable parsedData = new Hashtable();
				string xmlFile = AssetDatabase.GetAssetPath(instance.plistObject);;
				PListManager.ParsePListFile(xmlFile, ref parsedData);

				return parsedData;
			}
		}

		// Event types and variables
		public delegate void NewDataAvailable(Dictionary<string, object> snapshotDict, string action);
		public event NewDataAvailable ValueDataChanged;

		/// <summary>
		/// Initializes the singleton pattern and fires off Firebase init.
		/// </summary>
		void Awake()
		{
			if (instance && instance.GetInstanceID() != GetInstanceID())
				DestroyImmediate(gameObject);
			else
			{
				instance = this;
				DontDestroyOnLoad(gameObject);
			}

			InitializeFirebase();
		}

		/// <summary>
		/// Responsible for all Firebase setup.
		/// Not accessible from other scripts since it should only happen on Awake.
		/// </summary>
		public void InitializeFirebase() 
		{
			FirebaseApp.DefaultInstance.SetEditorDatabaseUrl(plist[Constants.PlistKeys.databaseURL].ToString());
		}

		/// <summary>
		/// Public entry point for requesting new Firebase data.
		/// </summary>
		/// <param name="childNodes">Array of database child nodes. If empty, reference is defaulted to root Firebase project reference.</param>
		public void LoadData() 
		{
			DatabaseReference constructedRef = ConstructReference();
//			if (sortBy != SortOption.None && sortValue != "")
//				AddReferenceQuery(constructedRef);
//			else
			constructedRef.OrderByChild("isAlive").ValueChanged += HandleValueChange;

			Debug.Log("Database queried at " + constructedRef);
		}

		/// <summary>
		/// Subscribes to Firebase value change event on current reference.
		/// Every value change emits a new event with formatted data string to all subscribers.
		/// </summary>
		/// <param name="sender">Sender object.</param>
		/// <param name="eventArgs">Event argument that holds Firebase DataSnapshot.</param>
		private void HandleValueChange(object sender, ValueChangedEventArgs eventArgs) 
		{
			// Map to new dictionary so sorting/filtering order is not lost
			var snapDict = new Dictionary<string, object>();
			foreach (var kvp in eventArgs.Snapshot.Children)
				snapDict[kvp.Key] = kvp.Value;

			if (ValueDataChanged != null)
				ValueDataChanged(snapDict, "Value Changed");
		}

		#region Utilities
		/// <summary>
		/// Constructs a Firebase DatabaseReference from child nodes parameters.
		/// </summary>
		/// <returns>A reference constructed from input child nodes from FBDebuggerWindow (Editor Window).</returns>
		/// <param name="childNodes">Child nodes.</param>
		private DatabaseReference ConstructReference() 
		{
			string childRef = string.Join("/", childNodes);
			return FirebaseDatabase.DefaultInstance.GetReference(childRef);
		}

		private void AddReferenceQuery(DatabaseReference mainRef)
		{
			DatabaseReference returnRef = mainRef;
			switch (sortBy)
			{
				case SortOption.Child:
					returnRef.OrderByChild(sortValue).ValueChanged += HandleValueChange;
					break;
				case SortOption.Key:
					returnRef.OrderByKey();
					break;
				case SortOption.Value:
					returnRef.OrderByValue();
					break;
				case SortOption.None:
					break;
			}
		}

		public void Reset()
		{
			childNodes = new string[0];
			sortBy = SortOption.None;
			sortValue = "";
			filterBy = FilterOption.None;
			filterValue = 0;
			LoadData();
		}
		#endregion
	}
}
