using System;

public interface IAgentCombat
{
    void StartAttack();
    void StopAttack();
    void Reload();
    void StartHeal(int amount, float duration);

    bool IsDead();
    bool IsReloading();
    bool IsHealing();

    int GetCurrentAmmo();
    int GetMaxAmmo(); // NEW
    float GetCurrentHealth();
    float GetMaxHealth();

    event Action OnAmmoEmptyEvent;
    event Action OnAmmoLowEvent;
    event Action OnDeathEvent;
}