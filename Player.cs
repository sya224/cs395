using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using QLearningFramework;

public class Player : MovingObject
{

    public int wallDamage = 1;
    public int pointsPerFood = 10;
    public int pointPerSoda = 20;
    public int currentState = 0;

    public float restartLevelDelay = 1f;
    public static System.Random rnd = new System.Random();

    public Dictionary<string, double[]> QValue = new Dictionary<string, double[]>();
    
    private Animator animator;
    private int food;
    private bool skipMove;
    private Transform target;
    private Dictionary<string, int> stateValue = new Dictionary<string, int>();
    private string currentX = "0";
    private string currentY = "0";
    private string preX = "0";
    private string preY = "0";
    private int cx = 0;
    private int cy = 0;
    private double alpha = 0.1;
    private double gamma = 0.9;
    private float timer = 0.0f;

    protected override void Start()
    {
     

        //Get a component reference to the Player's animator component
        animator = GetComponent<Animator>();

        //Get the current food point total stored in GameManager.instance between levels.
        food = GameManager.instance.playerFoodPoints;

        //Call the Start function of the MovingObject base class.
        base.Start();
    }


    //This function is called when the behaviour becomes disabled or inactive.
    private void OnDisable()
    {
        //When Player object is disabled, store the current local food total in the GameManager so it can be re-loaded in next level.
        GameManager.instance.playerFoodPoints = food;
    }

    private void Update()
    {

        if (!GameManager.instance.playerTurn) return;
        timer += Time.deltaTime;
        if (timer > 0.1f)
        {
            int horizontal = 0;
            int vertical = 0;
            int oneMove = 0;
            int chooseRandom = 0;
            double reward = -1;
            QLearning q = new QLearning();
            //QAction fromTo;
            //QState state;
            string stateName;
            string stateNameNext;
            //string which_action;
            System.Random rnd = new System.Random();
            double Qmax = 0;
            double q0 = 0;
            double q1 = 0;
            double q2 = 0;
            double q3 = 0;

            if (!QValue.ContainsKey(preX + ',' + preY))
            {
                double[] defaults = new double[] { 0.0, 0.0, 0.0, 0.0 };
                QValue.Add(preX + ',' + preY, defaults);

            }


            stateName = preX + "," + preY;
            chooseRandom = (int)rnd.Next(0, 10);
            if (chooseRandom <= 2)
            {
                oneMove = (int)rnd.Next(0, 4);
                if (oneMove == 0)
                {
                    horizontal = 1;
                    vertical = 0;
                    cx = cx + 1;
                    AttemptMove<Wall>(horizontal, vertical);

                }
                else if (oneMove == 1)
                {
                    horizontal = -1;
                    vertical = 0;
                    cx = cx - 1;
                    AttemptMove<Wall>(horizontal, vertical);

                }
                else if (oneMove == 2)
                {
                    vertical = 1;
                    horizontal = 0;
                    cy = cy + 1;
                    AttemptMove<Wall>(horizontal, vertical);

                }
                else
                {
                    vertical = -1;
                    horizontal = 0;
                    cy = cy - 1;
                    AttemptMove<Wall>(horizontal, vertical);
                }
            }
            
            else
            {
                
                q0 = QValue[stateName][0];
                q1 = QValue[stateName][1];
                q2 = QValue[stateName][2];
                q3 = QValue[stateName][3];
                if(q0 >= q1 && q0 >= q1 && q0 >= q2 && q0 >= q3)
                {
                    horizontal = 1;
                    vertical = 0;
                    cx = cx + 1;
                    AttemptMove<Wall>(horizontal, vertical);
                }
                else if(q1 >= q2 && q1 >= q3)
                {
                    horizontal = -1;
                    vertical = 0;
                    cx = cx - 1;
                    AttemptMove<Wall>(horizontal, vertical);
                }
                else if(q2 >= q3)
                {
                    vertical = 1;
                    horizontal = 0;
                    cy = cy + 1;
                    AttemptMove<Wall>(horizontal, vertical);
                }
                else
                {
                    vertical = -1;
                    horizontal = 0;
                    cy = cy - 1;
                    AttemptMove<Wall>(horizontal, vertical);
                }
            }
            currentX = cx.ToString();
            currentY = cy.ToString();
            if (!QValue.ContainsKey(currentX + ',' + currentY))
            {
                double[] defaults = new double[] { 0.0, 0.0, 0.0, 0.0 };
                QValue.Add(currentX + ',' + currentY, defaults);

            }


            // State Bedroom           
            stateName = preX + "," + preY;
            stateNameNext = currentX + "," + currentY;
            if (stateNameNext == "7,7")
            {
                reward = 100;
            }
            else
            {
                reward = -1;
            }
            Qmax = QValue[stateNameNext].Max();
            QValue[preX + ',' + preY][oneMove] = QValue[currentX + ',' + currentY][oneMove] + alpha * (reward + gamma * Qmax - QValue[currentX + ',' + currentY][oneMove]);



            preX = currentX;
            preY = currentY;
            horizontal = 0;
            vertical = 0;

            timer = 0.0f;
        }


    }
    protected override void AttemptMove<T>(int xDir, int yDir)
    {


        
        base.AttemptMove<T>(xDir, yDir);
            //food--;
   
         

      

         RaycastHit2D hit;

         CheckIfGameOver();
        GameManager.instance.playerTurn = false;

    }
 

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Exit")
        {
            Invoke("Restart", restartLevelDelay);
            enabled = false;
        }
        else if (other.tag == "Food")
        {
            food += pointsPerFood;
            other.gameObject.SetActive(false);

        }
        else if (other.tag == "Soda")
        {
            food += pointPerSoda;
            other.gameObject.SetActive(false);
        }
    }

    protected override void OnCantMove<T>(T component)
    {
        Wall hitWall = component as Wall;
        hitWall.DamageWall(wallDamage);
        animator.SetTrigger("playerChop");

        throw new System.NotImplementedException();
    }

    private void Restart()
    {
        Application.LoadLevel(Application.loadedLevel);
    }

    public void LossFood (int loss)
    {
        animator.SetTrigger("playerHit");
        food -= loss;
        CheckIfGameOver();
    }

    private void CheckIfGameOver()
    {
        if (food <= 0)
            GameManager.instance.GameOver();

    }


}

