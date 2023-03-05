using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnityChanFace : MonoBehaviour
{
    [SerializeField] private AnimationClip smile;
    [SerializeField] private AnimationClip conf;
    [SerializeField] private AnimationClip sap;
    [SerializeField] private AnimationClip angry;
    [SerializeField] private AnimationClip other;
    [Space(14)]
    [SerializeField] private AnimationClip pose;

    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
        anim.CrossFade(pose.name, 0);
    }

    public void setSmileFace() => anim.CrossFade(smile.name, 0);
    public void setConfFace() => anim.CrossFade(conf.name, 0);
    public void setSapFace() => anim.CrossFade(sap.name, 0);
    public void setAngryFace() => anim.CrossFade(angry.name, 0);
    public void setOtherFace() => anim.CrossFade(other.name, 0);

    void Update() => anim.SetLayerWeight(1, 1);

    public void OnCallChangeFace(string str)
    {
        var animation=new[] { smile, conf, sap, angry, other }.Where(item => item.name == str).FirstOrDefault();
        anim.CrossFade((animation != null) ? str : "default@unitychan", 0);
    }
}
