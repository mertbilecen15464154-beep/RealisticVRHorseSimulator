using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// XRGrabInteractable sýnýfýný türeterek Grip tuþuna tek basýþta tutma (Toggle)
/// iþlevselliði kazandýran özel Interactable sýnýfý.
/// </summary>
public class XRToggleGrabInteractable : XRGrabInteractable
{
    private bool m_IsToggledHeld = false;

    protected override void OnSelectEntering(SelectEnterEventArgs args)
    {
        // Eðer obje zaten tutuluyorsa ve grip tuþuna tekrar basýldýysa objeyi býrak
        if (m_IsToggledHeld)
        {
            m_IsToggledHeld = false;

            if (args.interactorObject is IXRSelectInteractor interactor)
            {
                interactionManager.SelectExit(interactor, this);
            }
            return;
        }

        // Ýlk basýþta objeyi tut ve Toggle durumunu aktif et
        m_IsToggledHeld = true;
        base.OnSelectEntering(args);
    }

    protected override void OnSelectExiting(SelectExitEventArgs args)
    {
        // Yalnýzca Toggle durumu tamamen sýfýrlandýðýnda býrakma iþlemini tamamla
        if (!m_IsToggledHeld)
        {
            base.OnSelectExiting(args);
        }
    }
}