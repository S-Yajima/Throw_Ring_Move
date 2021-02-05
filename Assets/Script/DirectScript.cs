using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class DirectScript : MonoBehaviour
{
    // リングのゲームオブジェクト。
    public GameObject ring_prefab_silver;
    public GameObject ring_prefab_red;
    public GameObject ring_prefab_green;
    private GameObject[] ring = new GameObject[3];
    // ポールのゲームオブジェクト
    private GameObject[] pole = new GameObject[4];

    // 回転軸回転角度
    private const float axis_rotate_y = -90.0f;
    // 最高到達高度
    private float max_height = 1.0f;
    // ポールの中心と先端の高さの差分
    private const float offset_pole_height = 0.25f;

    // 力を加える向きの単位ベクトル
    private Vector3 force_n_v3 = new Vector3(1.0f, 0.0f, 0.0f);
    // UnityのPhysicsで設定されている重力加速度の値を取得する。
    private float G = Mathf.Abs(Physics.gravity.y);

    // リングとポールの処理対象インデックス番号
    private int target_pole_index = 0;
    private int target_ring_index = 0;

    // NavMeshAgent用 目的地Z座標 と index
    private int target_nav_z_pos_index = 0;
    private float[] target_nav_z_pos = {0.5f, -3.76f};

    // 時間測定用の変数
    private float delta_time = 0.0f;
    private float interval_time = 5.0f;



    /*
    * リングからポールまで投射する初速度を算出する
    * 戻り値 : float 初速度 m/s の値
    * 当メソッドの戻り値と単位ベクトルをかけ、投射初速度のベクトルを生成する
    * 
    * 初速度 v / 投射角度 θ / 重力加速度 G とすると下記が成り立つ
    * 式) v * cosθ * ((v * sinθ) / G) * 2 = 水平到達距離
    * 
    * 上記の式を元に水平到達距離から初速度を算出する式は
    * v * cosθ * ((v * sinθ) / G) = 水平到達距離 / 2
    * ↓
    * v * v * cosθ = (水平到達距離 / 2) * (G / sinθ)
    * ↓
    * v * v = (水平到達距離 / 2) * (G / sinθ) * (1 / cosθ)
    * ↓
    * 式) v = Sqrt(水平到達距離 * G / (2 * cos * sin))
    */
    /*
    float first_velocity(float distance)
    {
        float velocity = 0.0f;
        if(this.force_cos_val != 0 && this.force_sin_val != 0)
        {
            velocity = Mathf.Sqrt(distance * this.G / (2 * this.force_cos_val * this.force_sin_val));
        }

        return velocity;
    }
    */


    /*
     * 滞空時間を算出する
     * 
     * 戻り値 : float 滞空時間
     * 
     * 入力 
     * float h : 投射する物体の到達最高地点
     * float h0 : 物体を投射する位置の高さ
     * 
     * 
     * 投射位置から落下地点(ポールの先端)までの滞空時間を「t」とする。 
     * 最高到達地点を境に「t」を分割して考える。
     * 「t1」: 投射位置 から 最高到達地点 までの滞空時間
     * 「t2」: 最高到達地点 から 落下地点 までの滞空時間
     * t = t1 + t2 を前提とする。
     * 投射地点の高さを仮に「h0」とする。
     * 
     * 投射地点 から 最高到達地点
     * h - h0 = 1/2 * G * t1^2
     * 
     * t1^2 = (2 * (h - h0)) / G
     * t1 = Sqrt((2 * (h - h0)) / G)
     * 
     * 最高到達地点 から 落下地点
     * h = 1/2 * G * t2^2
     * 
     * t2^2 = (2 * h) / G 
     * t2 = Sqrt((2 * h) / G) 
     * 
     * t = t1 + t2 から
     * 【式】t = Sqrt((2 * (h - h0)) / G) + Sqrt((2 * h) / G)
     * 
     * ↑の【式】で滞空時間を算出する。
     * 
     */
    float time_with_height(float h, float h0)
    {
        return Mathf.Sqrt(2.0f * h / this.G) + Mathf.Sqrt(2.0f * (h - h0) / this.G);

        // ↓ 分母の有理化を実行した場合の式。下記でも結果は同じ。
        //return (Mathf.Sqrt(2.0f * this.G * (h - h0)) + Mathf.Sqrt(2.0f * this.G * h)) / this.G;
    }


    /*
     * 初速度を算出する
     * 
     * 戻り値: float 初速度
     * 
     * 入力:
     * float h  : 投射後の最高到達地点
     * float h0 : 投射地点の高さ
     * float l  : 投射地点から到達地点までのX軸平面投射距離
     * float t  : 
     * 
     * 
     * ３平方の定理より下記の方針で計算式を考える。
     * v = Sqrt(vx^2 + vy^2)
     * v  : 投射初速度。算出対象。
     * vx : 投射初速度のx成分
     * vy : 投射初速度のy成分
     * 
     * vxを算出する。
     * l を投射位置から到達地点までのX軸平面距離、t を到達までの滞空時間とする。
     * vx = l / t
     * 
     * vyを算出する。
     * -1 * h0 = (vy * t) - (1/2 * G * t^2)     ※ 左辺の -符号に注意
     * vy * t = (-1 * h0) + (1/2 * G * t^2)
     * vy = (1/2 * G * t) - (h0 / t)
     * 
     * 上記にて算出したvxとvyを元に
     * 【式】v = Sqrt((l / t)^2 + ((1/2 * G * t) - (h0 / t))^2)
     * 
     * ↑の【式】で初速度を算出する。
     * 
     */
    float velocity_with_height_length_time(float h, float h0, float l, float t)
    {
        float v = 0.0f;

        if (t != 0)
        {
            float vx = l / t;
            float vy = (0.5f * this.G * t) - (h0 / t);
            v = Mathf.Sqrt(Mathf.Pow(vx, 2) + Mathf.Pow(vy, 2));
        }
        return v;

        // ↓サイトの情報から参考にした計算式。下記でも正常に動作する。
        // return Mathf.Sqrt(Mathf.Pow((l / t), 2) + 2.0f * this.G * (h - h0));
    }


    /*
     * 角度を算出する
     * 
     * 戻り値 : float 投射角度。degree値
     * 
     * 入力値
     * float h　: 最高到達高度
     * float h0 : 投射地点高度
     * float l  : 水平到達距離
     * float t  : 滞空時間
     * 
     * v : 初速
     * vx : 初速X軸要素
     * vy : 初速Y軸要素
     * 上記を前提とし下記の式で投射角度を算出する。
     * θ = Atan(vy / vx)
     * 
     * 時間 t 毎の 速度のX要素 vx と Y要素 vy の値を求める式を考える。
     * 
     * vx を算出)
     * vx = l / t
     * 
     * vy を算出)
     * -h0 = (vy * t) - (1/2 * G * t^2)
     * vy * t = (1/2 * G * t^2) - h0
     * vy = (1/2 * G * t) - (h0 / t)
     * 
     * 算出した vx と vy ともに t をかける。
     * vx = l
     * vy = (1/2 * G * t^2) - h0
     * 
     * 下記【式】にて投射角度を計算する。
     * 【式】θ = Atan(((1/2 * G * t^2) - h0) / l)
     * 
     */
    float theta_with_maxh_h(float h, float h0, float l, float t)
    {
        float angle_rad = 0.0f;
        if (l > 0)
        {
            angle_rad = Mathf.Atan(((0.5f * this.G * t * t) - h0) / l);

            // ↓ サイトにて参考にした式。下記式でも輪投げは成功する。
            // angle_rad = Mathf.Atan((t * Mathf.Sqrt(2.0f * this.G * (h - h0))) / l);
        }
        float theta = angle_rad * Mathf.Rad2Deg;
        return theta;
    }


    /*
     * 対象インデックスをアップデートする
     */
    void update_target_index()
    {
        // 投射対象のポールのインデックスをインクリメントする
        this.target_pole_index += 1;
        if (this.target_pole_index >= this.pole.Length)
        {
            // 対象ポールのインデックス番号がMAXに達したら0に戻す
            this.target_pole_index = 0;
            this.interval_time = 4.0f;

            // 対象ポールのインデックス番号がMAXに達したら投射角度を変更する
            this.max_height += 1.0f;
            if (this.max_height > 3.1f) this.max_height = 1.0f;
        }
        else
        {
            this.interval_time = 1.5f;
        }

        // 投射対象のリングのインデックスを更新する
        this.target_ring_index += 1;
        if (this.target_ring_index >= this.ring.Length)
        {
            this.target_ring_index = 0;
        }
    }

    /*
     * リングを投射する
     */
    void throw_ring()
    {
        // リングとポールの位置を取得する
        Vector3 ring_pos_v3 = this.ring[this.target_ring_index].transform.position;
        Vector3 pole_pos_v3 = this.pole[this.target_pole_index].transform.position;

        // リング投射位置の高さ と ポール到達高度 の差分
        float diff_height = ring_pos_v3.y - (pole_pos_v3.y + DirectScript.offset_pole_height);
        // リングとポールの位置の高さを揃える
        pole_pos_v3.y = ring_pos_v3.y;
        // リングからポールまでのベクトル(距離)を取得する
        Vector3 distance_v3 = pole_pos_v3 - ring_pos_v3;

        // 滞空時間を算出する
        float time = time_with_height(this.max_height, diff_height);
        // 初速度をfloatで算出する
        float velocity = velocity_with_height_length_time(this.max_height, diff_height, distance_v3.magnitude, time);
        // 投射角度を算出する。
        float theta = theta_with_maxh_h(this.max_height, diff_height, distance_v3.magnitude, time);

        // 初期投射速度の回転軸を作る
        Vector3 role_axis_v3 = Quaternion.Euler(0, DirectScript.axis_rotate_y, 0) * distance_v3.normalized;

        // リングを投射する
        // 加える力のベクトルを生成する
        Vector3 force_v3 = Quaternion.AngleAxis((int)theta, role_axis_v3) * (distance_v3.normalized * velocity);
        // 力を加えて放出する. ForceMode.VelocityChange
        GameObject ring = Instantiate(this.ring[this.target_ring_index]);
        ring.GetComponent<Rigidbody>().AddForce(force_v3, ForceMode.VelocityChange);
    }


    /*
     * AI にてポールの動作を制御する
     */
    void move_navigation_pole()
    {
        NavMeshAgent nav = this.pole[3].GetComponent<NavMeshAgent>();
        if (nav != null)
        {
            nav.destination = new Vector3(1.93f, -0.25f, this.target_nav_z_pos[this.target_nav_z_pos_index]);
            this.target_nav_z_pos_index = ++this.target_nav_z_pos_index % this.target_nav_z_pos.Length;
        }
    }


    /*
    * 一定時間毎に自動で実行される。
    * 物理演算は当メソッドで実行する。
    */
    void FixedUpdate()
    {
        // 1秒間ごとにリング投射。インデックスの分投げ切った後、4秒休止。
        this.delta_time += Time.deltaTime;
        if (this.interval_time <= this.delta_time)
        {
            // デルタタイムを初期化する。
            this.delta_time = 0.0f;
            // リングを投射する。
            throw_ring();
            // 投射対象ポールインデックスを更新する。
            update_target_index();
            // ポールを動かす
            PoleController ctrl = this.pole[this.target_pole_index].GetComponent<PoleController>();
            if (ctrl != null)
            {
                ctrl.move();
            }
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        // ポールをメンバ変数(配列)に取得する。
        this.pole[0] = GameObject.Find("Pole_r_003_h_050_1");
        this.pole[1] = GameObject.Find("Pole_r_003_h_050_2");
        this.pole[2] = GameObject.Find("Pole_r_003_h_050_3");
        this.pole[3] = GameObject.Find("Pole_r_003_h_050_4");

        // リングをメンバ変数(配列)に取得する。
        this.ring[0] = this.ring_prefab_silver;
        this.ring[1] = this.ring_prefab_red;
        this.ring[2] = this.ring_prefab_green;

        // Navigation AI を制御するメソッドの実行を設定する。
        InvokeRepeating("move_navigation_pole", 0.0f, 12.0f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
