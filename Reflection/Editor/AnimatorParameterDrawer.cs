﻿using Ludiq.Controls;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Ludiq.Reflection
{
	[CustomPropertyDrawer(typeof(AnimatorParameter))]
	public class AnimatorParameterDrawer : TargetedDrawer
	{
		#region Fields

		/// <summary>
		/// The inspected property, of type AnimatorParameter.
		/// </summary>
		protected SerializedProperty property;

		/// <summary>
		/// The UnityMember.name of the inspected property, of type string.
		/// </summary>
		protected SerializedProperty nameProperty;

		/// <summary>
		/// The targeted animators.
		/// </summary>
		protected Animator[] targets;

		#endregion

		/// <inheritdoc />
		protected override void Update(SerializedProperty property)
		{
			// Update the targeted drawer
			base.Update(property);

			// Assign the property and sub-properties
			this.property = property;
			nameProperty = property.FindPropertyRelative("_name");

			// Find the targets
			targets = FindTargets();
		}

		/// <inheritdoc />
		protected override void RenderMemberControl(Rect position)
		{
			var options = GetNameOptions();

			PopupOption<AnimatorParameter> selectedOption = null;
			PopupOption<AnimatorParameter> noneOption = new PopupOption<AnimatorParameter>(null, "No Parameter");

			AnimatorParameter current = GetValue();

			if (current != null)
			{
				string label = current.name;
				selectedOption = new PopupOption<AnimatorParameter>(current, label);
			}

			// Make sure the callback uses the property of this drawer, not at its later value.
			var propertyNow = property;

			bool enabled = targets.Any(target => target != null);

			if (!enabled) EditorGUI.BeginDisabledGroup(true);

			PopupGUI<AnimatorParameter>.Render
			(
				position,
				value =>
				{
					Update(propertyNow);
					SetValue(value);
					propertyNow.serializedObject.ApplyModifiedProperties();
				},
				options,
				selectedOption,
				noneOption,
				nameProperty.hasMultipleDifferentValues
			);

			if (!enabled) EditorGUI.EndDisabledGroup();
		}

		/// <summary>
		/// Returns an animator parameter constructed from the current property values.
		/// </summary>
		protected AnimatorParameter GetValue()
		{
			if (nameProperty.hasMultipleDifferentValues || string.IsNullOrEmpty(nameProperty.stringValue))
			{
				return null;
			}

			string name = nameProperty.stringValue;
			if (name == string.Empty) name = null;
			return new AnimatorParameter(name);
		}

		/// <summary>
		/// Assigns the property values from a specified animator parameter.
		/// </summary>
		protected void SetValue(AnimatorParameter value)
		{
			if (value != null)
			{
				nameProperty.stringValue = value.name;
			}
			else
			{
				nameProperty.stringValue = null;
			}
		}

		/// <summary>
		/// Gets the list of targets on the inspected objects.
		/// </summary>
		protected Animator[] FindTargets()
		{
			IEnumerable<Object> objects;

			if (isSelfTargeted)
			{
				// In self targeting mode, the objects are the inspected objects themselves.

				objects = property.serializedObject.targetObjects;
			}
			else
			{
				// In manual targeting mode, the targets the values of each target property.

				objects = targetProperty.Multiple().Select(p => p.objectReferenceValue);
			}

			var childrenAnimators = objects.OfType<GameObject>().SelectMany(gameObject => gameObject.GetComponents<Animator>());
			var siblingAnimators = objects.OfType<Component>().SelectMany(component => component.GetComponents<Animator>());

			return childrenAnimators.Concat(siblingAnimators).ToArray();
		}

		/// <summary>
		/// Gets the list of shared parameter names as popup options.
		/// </summary>
		protected List<PopupOption<AnimatorParameter>> GetNameOptions()
		{
			var options = new List<PopupOption<AnimatorParameter>>();

			List<string> names = targets
				.Select(animator => ((AnimatorController)animator.runtimeAnimatorController))
				.Where(animatorController => animatorController != null)
				.Select(animatorController => animatorController.parameters)
				.Select(parameters => parameters.Select(parameter => parameter.name))
				.IntersectAll()
				.Distinct()
				.ToList();

			foreach (string name in names)
			{
				options.Add(new PopupOption<AnimatorParameter>(new AnimatorParameter(name), name));
			}

			return options;
		}
	}
}