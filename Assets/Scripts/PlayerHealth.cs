using Player;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barre de vie (valeur + couleurs), explosion à la mort, bouclier / invincibilité.
/// Les PV sont dans <see cref="Player.Player"/> ; place le Canvas UI où tu veux dans la scène (Screen Space, etc.).
/// </summary>
[RequireComponent(typeof(Player.Player))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Barre de vie")]
    public Slider m_Slider;
    public Image m_FillImage;
    public Color m_FullHealthColor = Color.green;
    public Color m_ZeroHealthColor = Color.red;

    [Header("Mort")]
    public GameObject m_ExplosionPrefab;

    [HideInInspector] public bool m_HasShield;

    private AudioSource m_ExplosionAudio;
    private ParticleSystem m_ExplosionParticles;
    private bool m_Dead;
    private float m_ShieldValue;
    private bool m_IsInvincible;

    private Player.Player _player;

    private void Awake()
    {
        _player = GetComponent<Player.Player>();
        if (_player == null)
        {
            enabled = false;
            return;
        }

        if (m_ExplosionPrefab != null)
        {
            m_ExplosionParticles = Instantiate(m_ExplosionPrefab).GetComponent<ParticleSystem>();
            m_ExplosionAudio = m_ExplosionParticles.GetComponent<AudioSource>();
            m_ExplosionParticles.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (m_ExplosionParticles != null)
            Destroy(m_ExplosionParticles.gameObject);
    }

    private void OnEnable()
    {
        m_Dead = false;
        m_HasShield = false;
        m_ShieldValue = 0f;
        m_IsInvincible = false;
        RefreshHealthUI();
    }

    /// <summary>Dégâts entrants : modificateurs bouclier / invincibilité.</summary>
    public float ModifyIncomingDamage(float amount)
    {
        if (m_IsInvincible) return 0f;
        if (m_HasShield && m_ShieldValue > 0f)
            return amount * (1f - m_ShieldValue);
        return amount;
    }

    /// <summary>À appeler depuis Player après chaque changement de PV.</summary>
    public void RefreshHealthUI()
    {
        if (_player == null)
            _player = GetComponent<Player.Player>();
        if (_player == null) return;

        if (m_Slider != null)
        {
            m_Slider.maxValue = Mathf.Max(0.01f, _player.MaxHealth);
            m_Slider.value = _player.CurrentHealth;
        }

        if (m_FillImage != null && _player.MaxHealth > 0f)
        {
            float t = Mathf.Clamp01(_player.CurrentHealth / _player.MaxHealth);
            m_FillImage.color = Color.Lerp(m_ZeroHealthColor, m_FullHealthColor, t);
        }
    }

    /// <summary>Mort du joueur : effets puis désactivation du tank (compat tutoriel Tanks).</summary>
    public void HandleDeath()
    {
        if (m_Dead) return;
        m_Dead = true;

        if (m_ExplosionParticles != null)
        {
            m_ExplosionParticles.transform.position = transform.position;
            m_ExplosionParticles.gameObject.SetActive(true);
            m_ExplosionParticles.Play();
            if (m_ExplosionAudio != null)
                m_ExplosionAudio.Play();
        }

        gameObject.SetActive(false);
    }

    /// <summary>Compat ancien code / tests : délègue à <see cref="Player.Player.TakeDamage"/>.</summary>
    public void TakeDamage(float amount)
    {
        if (_player != null)
            _player.TakeDamage(amount);
    }

    public void IncreaseHealth(float amount)
    {
        if (_player != null)
            _player.Heal(amount);
    }

    public void ToggleShield(float shieldAmount)
    {
        m_HasShield = !m_HasShield;
        m_ShieldValue = m_HasShield ? Mathf.Clamp01(shieldAmount) : 0f;
    }

    public void ToggleInvincibility()
    {
        m_IsInvincible = !m_IsInvincible;
    }
}
