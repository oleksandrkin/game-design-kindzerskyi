using UnityEngine;
 
public class ChaseState : IState
{
    private readonly EnemyController ctx;
    private float lostTimer;
 
    public ChaseState(EnemyController ctx) => this.ctx = ctx;
 
    public void Enter() => lostTimer = 0f;
 
    public void Tick()
    {
        float d = ctx.DistanceToTarget;
 
        // Ціль втрачено, якщо вона задалеко АБО на іншому рівні.
        // Множник — гістерезис: поріг виходу більший за поріг входу
        bool lost = d > ctx.Stats.chaseRange * ctx.Stats.chaseExitFactor
                    || !ctx.TargetOnSameLevel;
 
        if (lost)
        {
            // Не кидаємо переслідування миттєво: гравець міг стрибнути
            lostTimer += Time.deltaTime;
 
            if (lostTimer >= ctx.Stats.loseTargetTime)
            {
                ctx.Machine.ChangeState(ctx.PatrolState);
                return;
            }
        }
        else
        {
            lostTimer = 0f;
        }
 
        ctx.FaceTarget();
 
        // Впритул не підбігаємо: інакше ворог проскакує гравця,
        // розвертається, проскакує знову — і тремтить на місці.
        // У стіну теж не впираємось: там ми однаково не пройдемо,
        // але розвертатися НЕ можна — ціль саме в тому боці
        bool canAdvance = ctx.DistanceToTarget > ctx.Stats.stopDistance
                          && !ctx.AtWall();
 
        if (canAdvance) ctx.Move(ctx.Stats.chaseSpeed);
        else            ctx.Stop();
 
        if (d <= ctx.Stats.attackRange && ctx.TargetOnSameLevel)
            ctx.Machine.ChangeState(ctx.AttackState);
    }
 
    public void Exit() => ctx.Stop();
}