using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[DisallowMultipleComponent]
[RequireComponent(typeof(XRBaseInteractable))]
public class DirectOnlyInteractionFilter : MonoBehaviour, IXRHoverFilter, IXRSelectFilter
{
    readonly List<Collider> nearColliders = new List<Collider>();

    XRBaseInteractable interactable;
    bool filtersRegistered;

    public bool canProcess => isActiveAndEnabled;

    public static void ApplyTo(XRBaseInteractable target)
    {
        if (target != null &&
            target.GetComponent<DirectOnlyInteractionFilter>() == null)
        {
            target.gameObject.AddComponent<DirectOnlyInteractionFilter>();
        }
    }

    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
    }

    void OnEnable()
    {
        if (interactable == null)
            interactable = GetComponent<XRBaseInteractable>();

        if (interactable == null || filtersRegistered)
            return;

        interactable.hoverFilters.Add(this);
        interactable.selectFilters.Add(this);
        filtersRegistered = true;
    }

    void OnDisable()
    {
        if (interactable == null || !filtersRegistered)
            return;

        interactable.hoverFilters.Remove(this);
        interactable.selectFilters.Remove(this);
        filtersRegistered = false;
    }

    public bool Process(
        IXRHoverInteractor interactor,
        IXRHoverInteractable hoverInteractable)
    {
        return IsDirectInteraction(interactor);
    }

    public bool Process(
        IXRSelectInteractor interactor,
        IXRSelectInteractable selectInteractable)
    {
        return IsDirectInteraction(interactor);
    }

    bool IsDirectInteraction(object interactor)
    {
        if (interactor is XRRayInteractor)
            return false;

        if (interactor is NearFarInteractor nearFarInteractor)
            return IsNearTarget(nearFarInteractor);

        return true;
    }

    bool IsNearTarget(NearFarInteractor nearFarInteractor)
    {
        if (!nearFarInteractor.enableNearCasting ||
            nearFarInteractor.nearInteractionCaster == null ||
            nearFarInteractor.interactionManager == null)
        {
            return false;
        }

        nearColliders.Clear();

        if (!nearFarInteractor.nearInteractionCaster.TryGetColliderTargets(
                nearFarInteractor.interactionManager,
                nearColliders))
        {
            return false;
        }

        foreach (Collider candidateCollider in nearColliders)
        {
            if (nearFarInteractor.interactionManager.TryGetInteractableForCollider(
                    candidateCollider,
                    out IXRInteractable candidateInteractable) &&
                ReferenceEquals(candidateInteractable, interactable))
            {
                return true;
            }
        }

        return false;
    }
}
