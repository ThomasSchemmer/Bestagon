using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** Allows tagging of methods specifically for use in questables */
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class QuestableAttribute : Attribute {}
