using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;

// STRATEGY:
// For the final strategy there were three different components. 
// One is that it does not pick up more than three balls at once so not to go too
// slow and be able to be frozen and shot. Second was that in the last 
// seconds we would go into the enemy base and take the targets in their base. 
// Finally,  to return to our base with all the balls from the enemy base(MissionImpossible function). 
// This way we collect balls quickly at the start, then towards the end steal their hopes of victory.


public class Rotten_Apple : CogsAgent
{
    // ------------------BASIC MONOBEHAVIOR FUNCTIONS-------------------
    protected int enemyInfo;
    protected GameObject enemyBase;
    
    // Initialize values
    protected override void Start()
    {
        base.Start();
        AssignBasicRewards();
        if (team == 1) {
            enemyInfo = 2;
        } else {

            enemyInfo = 1;
        }

        enemyBase = GameObject.Find("Base " + enemyInfo);

        
    }

    // For actual actions in the environment (e.g. movement, shoot laser)
    // that is done continuously
    protected override void FixedUpdate() {
        base.FixedUpdate();
        
        LaserControl();
        // Movement based on DirToGo and RotateDir
        if(!IsFrozen()){
            if (!IsLaserOn()){
                rBody.AddForce(dirToGo * GetMoveSpeed(), ForceMode.VelocityChange);
            }
            transform.Rotate(rotateDir, Time.deltaTime * GetTurnSpeed());
        }
        
    }


    
    // --------------------AGENT FUNCTIONS-------------------------

    // Get relevant information from the environment to effectively learn behavior
    public override void CollectObservations(VectorSensor sensor)
    {
        // Agent velocity in x and z axis 
        var localVelocity = transform.InverseTransformDirection(rBody.velocity);
        sensor.AddObservation(localVelocity.x);
        sensor.AddObservation(localVelocity.z);

        // Time remaning
        sensor.AddObservation(timer.GetComponent<Timer>().GetTimeRemaning());  

        // Agent's current rotation
        var localRotation = transform.rotation;
        sensor.AddObservation(transform.rotation.y);

        // Agent and home base's position
        sensor.AddObservation(this.transform.localPosition);
        sensor.AddObservation(baseLocation.localPosition);

        // for each target in the environment, add: its position, whether it is being carried,
        // and whether it is in a base
        foreach (GameObject target in targets){
            sensor.AddObservation(target.transform.localPosition);
            sensor.AddObservation(target.GetComponent<Target>().GetCarried());
            sensor.AddObservation(target.GetComponent<Target>().GetInBase());
        }
        
        // Whether the agent is frozen
        sensor.AddObservation(IsFrozen());
    }

    // For manual override of controls. This function will use keyboard presses to simulate output from your NN 
    public override void Heuristic(float[] actionsOut)
    {
        var discreteActionsOut = actionsOut;
        discreteActionsOut[0] = 0; //Simulated NN output 0
        discreteActionsOut[1] = 0; //....................1
        discreteActionsOut[2] = 0; //....................2
        discreteActionsOut[3] = 0; //....................3

        //TODO-2: Uncomment this next line when implementing GoBackToBase();
        discreteActionsOut[4] = 0;

       
        if (Input.GetKey(KeyCode.UpArrow))
        {
            discreteActionsOut[0] = 1;
        }       
        if (Input.GetKey(KeyCode.DownArrow))
        {
            discreteActionsOut[0] = 2;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            discreteActionsOut[1] = 1;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            //TODO-1: Using the above as examples, set the action out for the left arrow press
            discreteActionsOut[1] = 2;
        }
        

        //Shoot
        if (Input.GetKey(KeyCode.Space)){
            discreteActionsOut[2] = 1;
        }

        //GoToNearestTarget
        if (Input.GetKey(KeyCode.A)){
            discreteActionsOut[3] = 1;
        }


        //TODO-2: implement a keypress (your choice of key) for the output for GoBackToBase();
        if (Input.GetKey(KeyCode.B)) {
            discreteActionsOut[4] = 1;
        }

    }

        // What to do when an action is received (i.e. when the Brain gives the agent information about possible actions)
    public override void OnActionReceived(float[] act)
    {
        int forwardAxis = (int)act[0]; //NN output 0

        //TODO: Set these variables to their appopriate item from the act list
        int rotateAxis = (int)act[1]; 
        int shootAxis = (int)act[2]; 
        int goToTargetAxis = (int)act[3]; 
        
        //TODO-2: Uncomment this next line and set it to the appropriate item from the act list
        int goToBaseAxis = (int)act[4];

        //TODO-2: Make sure to remember to add goToBaseAxis when working on that part!
        MovePlayer(forwardAxis, rotateAxis, shootAxis, goToTargetAxis, goToBaseAxis);


        //test to check 
       // Debug.Log(carriedTargets.Count);

    
        

        
        //Go to Enemy Base and steal its targets
        if(timer.GetComponent<Timer>().GetTimeRemaning() < 60){
            MissionImpossible();
        }

        if (carriedTargets.Count > 2) {
            GoToBase();
        }

      



        //Robot does not go below 0.85 speed because it is easy target.
        if(GetMoveSpeed()<0.85) {
            AddReward(-0.01f);
        }   


       
    }
// ----------------------ONTRIGGER AND ONCOLLISION FUNCTIONS------------------------
    // Called when object collides with or trigger (similar to collide but without physics) other objects
    protected override void OnTriggerEnter(Collider collision)
    {
        base.OnTriggerEnter(collision);

        

        if (collision.gameObject.CompareTag("HomeBase") && carriedTargets.Count == 0  )
        {

            //Basic Reward for going to base without a ball
            AddReward(-0.2f);
        }

        if (collision.gameObject.CompareTag("HomeBase") && carriedTargets.Count > 0 && (timer.GetComponent<Timer>().GetTimeRemaning() > 60))
        {
            
            //Basic Reward for going to base with more than one ball
            AddReward(1.0f);
        }
    }



    protected override void OnCollisionEnter(Collision collision) 
    {
        base.OnCollisionEnter(collision);


        if (collision.gameObject.CompareTag("Wall"))
        {
            // Reward for not hitting the wall 
            AddReward(-0.02f);
        }

        if (collision.gameObject.CompareTag("Target") && (timer.GetComponent<Timer>().GetTimeRemaning() > 60))
        {   
            //Basic reward for grabbing target. 
            AddReward(1.0f);
        }

        if (collision.gameObject.CompareTag("Target") && carriedTargets.Count == 3 && (timer.GetComponent<Timer>().GetTimeRemaning() > 60))
        {   
            //Basic reward for grabbing target. 
            AddReward(1.5f);
        }
    }



    //  --------------------------HELPERS---------------------------- 
     private void AssignBasicRewards() {
        rewardDict = new Dictionary<string, float>();

        rewardDict.Add("frozen", -0.2f);
        rewardDict.Add("shooting-laser", 0f);
        rewardDict.Add("hit-enemy", 0.01f);
        rewardDict.Add("dropped-one-target", -0.1f);
        rewardDict.Add("dropped-targets", -0.2f);

    }
    
    private void MovePlayer(int forwardAxis, int rotateAxis, int shootAxis, int goToTargetAxis, int goToBaseAxis)
    //TODO-2: Add goToTargetAxis as an argument to this function ^
    {
        dirToGo = Vector3.zero;
        rotateDir = Vector3.zero;

        Vector3 forward = transform.forward;
        Vector3 backward = -transform.forward;
        Vector3 right = transform.up;
        Vector3 left = -transform.up;

        //fowardAxis: 
            // 0 -> do nothing
            // 1 -> go forward
            // 2 -> go backward
        if (forwardAxis == 0){
            //do nothing. This case is not necessary to include, it's only here to explicitly show what happens in case 0
        }
        else if (forwardAxis == 1){
            dirToGo = forward;
        }
        else if (forwardAxis == 2){
            dirToGo = backward;
        }

        //rotateAxis: 
            // 0 -> do nothing
            // 1 -> go right
            // 2 -> go left
        if (rotateAxis == 0){
            //do nothing
        } else if (rotateAxis == 1) {
            //move right
            rotateDir = right;
        } else if (rotateAxis == 2) {
            rotateDir = left;
        }
        //TODO-1 : Implement the other cases for rotateDir


        //shoot
        if (shootAxis == 1){
            SetLaser(true);
        } else {
            SetLaser(false);
        }

        //go to the nearest target
        if (goToTargetAxis == 1){
            GoToNearestTarget();
        }

        //TODO-2: Implement the case for goToBaseAxis
        if (goToBaseAxis == 1) {
            GoToBase();
        }

        
    }

    // Go to home base
    private void GoToBase(){
        TurnAndGo(GetYAngle(myBase));
    }

    //Added Helper to go to enemy base
    private void GoToEnemyBase() {
        TurnAndGo(GetYAngle(enemyBase));
        
    }

    //Helper for executing the snake strategy to steal enemy targets.
    private void MissionImpossible() {

        if (timer.GetComponent<Timer>().GetTimeRemaning() < 17) {
            //Go back to base with stolen balls
            GoToBase();
            SetLaser(false);
        } else if (timer.GetComponent<Timer>().GetTimeRemaning() < 40) {
            //Steal what is in their base
            GoToNearestTarget();
        } else if (timer.GetComponent<Timer>().GetTimeRemaning() < 50) {
            //Go to enemy base to start to steal
            
            GoToEnemyBase();
            SetLaser(false);
        } else if (timer.GetComponent<Timer>().GetTimeRemaning() < 60) {
            //Go back to base to deposit what we have
            GoToBase();
            SetLaser(false);
        }

    }

    // Go to the nearest target
    private void GoToNearestTarget(){
        GameObject target = GetNearestTarget();
        if (target != null){
            float rotation = GetYAngle(target);
            TurnAndGo(rotation);
        }        
    }

    // Rotate and go in specified direction
    private void TurnAndGo(float rotation){

        if(rotation < -5f){
            rotateDir = transform.up;
        }
        else if (rotation > 5f){
            rotateDir = -transform.up;
        }
        else {
            dirToGo = transform.forward;
        }
    }

    // return reference to nearest target
    protected GameObject GetNearestTarget(){
        float distance = 200;
        GameObject nearestTarget = null;
        foreach (var target in targets)
        {
            float currentDistance = Vector3.Distance(target.transform.localPosition, transform.localPosition);
            if (currentDistance < distance && target.GetComponent<Target>().GetCarried() == 0 && target.GetComponent<Target>().GetInBase() != team){
                distance = currentDistance;
                nearestTarget = target;
            }
        }
        return nearestTarget;
    }

    private float GetYAngle(GameObject target) {
        
       Vector3 targetDir = target.transform.position - transform.position;
       Vector3 forward = transform.forward;

      float angle = Vector3.SignedAngle(targetDir, forward, Vector3.up);
      return angle; 
        
    }
}
