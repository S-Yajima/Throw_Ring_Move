using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoleController : MonoBehaviour
{
    // 上昇下降開始フラグ
    private bool move_flg = false;
    // 上昇下降速度。マイナスが下降。
    private float velocity = -2.0f;
    // 上昇下降を実施した移動距離。移動するたびに加算する。
    private float move_length = 0.0f;
    // 上昇下降の移動距離。指定した距離分、上昇または下降したら動作を終了する。
    private float max_move_length = 1.0f;

    // Start is called before the first frame update



    /*
     * 上昇下降速度を設定する
     */
    public void set_velocity(float velocity)
    {
        this.velocity = velocity;
    }


    /*
     * 上昇下降を実行する
     */
    public void move()
    {
        this.move_flg = true;
    }


    void Start()
    {
        this.move_flg = false;
    }


    // Update is called once per frame
    void Update()
    {
        // フラグがTrueの場合、上昇下降を実行する。
        if (this.move_flg == true)
        {
            float l = velocity * Time.deltaTime;
            transform.Translate(0.0f, 0.0f, l);
            this.move_length += Mathf.Abs(l);
            if(this.move_length >= this.max_move_length)
            {
                this.move_flg = false;
                this.move_length = 0.0f;
                this.velocity *= -1;
            }
        }
    }
}
