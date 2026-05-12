# Wrecking Ball Game

Unity проект развиен од: Матеј Стефановски 243103 и Димитар Урумов 243093  

---

# 1. Објаснување на проектот

`Wrecking Ball Game` е 2D аркадна игра изработена во Unity Engine. Играчот управува со подвижна платформа (кран) која контролира wrecking-ball топка. Главната цел е уништување на сите блокови пред истекот на времето.

Играта е инспирирана од класичните arcade breakout механики, но со изменет gameplay и wrecking-ball тематика.

---

# 2. Главни функционалности

## 2.1 Gameplay систем
- Движење на платформата лево/десно.
- Одбивање на топката од ѕидови, блокови и платформата.
- Ограничено време за завршување на нивото.
- Автоматски win/lose систем.
- Progressive difficulty system:
  - По секоја победа:
    - времето се намалува за 5 секунди,
    - брзината на топката се зголемува за 1.2x.
- Counter за успешно поминати нивоа.

---

## 2.2 Block систем
Секој блок има HP вредност:
- Некои блокови се уништуваат со еден удар.
- Посилните блокови бараат повеќе удари.

Имплементирани се:
- визуелни cracks ефекти,
- различни overlay sprites,
- автоматско уништување кога HP ќе достигне 0.

Пример:
```csharp
public void TakeDamage(int damageAmount)
{
    currentHp -= damageAmount;

    if (currentHp <= 0)
    {
        Destroy(gameObject);
    }
}
```

---

## 2.3 Powerup систем

Во секое ниво се крие:
- 1 `Money` powerup
- 1 `Hammer` powerup

Powerup-ите се доделуваат случајно на блокови.

### Money Powerup
- Платформата станува поголема.
- Се менува sprite-от.
- Ефектот трае 5 секунди.

### Hammer Powerup
- Топката прави двојна штета.
- Се менува sprite-от на топката.
- Ефектот трае 5 секунди.

Пример:
```csharp
ballController.ApplyMetalBall(powerupDuration, metalBallSprite);
```

---

## 2.4 Audio систем
Имплементирани се:
- Bounce sound effects
- Block hit sound effects
- Powerup pickup sounds
- Win/Lose sound effects
- Background music за menu и gameplay scene

---

## 2.5 UI систем
UIManager автоматски:
- го прикажува преостанатото време,
- бројот на блокови,
- бројот на поминати нивоа,
- Win screen,
- Game Over screen.

Пример:
```csharp
uiManager.UpdateTimer(currentTime);
uiManager.UpdateBlocksRemaining(blocksRemaining);
```

---

# 3. Контроли

| Копче | Функција |
|---|---|
| A / D | Движење лево/десно |
| Left / Right Arrow | Движење лево/десно |
| Space | Старт / рестарт / следно ниво |
| Esc | Враќање во Main Menu |
| Z | Debug auto-win |

---

# 4. Архитектура на проектот

Проектот е поделен на повеќе независни Unity скрипти.

## 4.1 GameManager
Главна класа задолжена за:
- тајмер,
- game state,
- win/lose логика,
- progression system,
- рестартирање на ниво.

Главни променливи:
```csharp
[SerializeField] private float startTimeSeconds = 60f;
[SerializeField] private BallController ballController;
[SerializeField] private UIManager uiManager;
```

---

## 4.2 BallController
Контролира:
- движење на топката,
- collision detection,
- bounce physics,
- штета врз блокови,
- visual chain renderer.

Пример:
```csharp
moveDirection = Vector2.Reflect(moveDirection, hit.normal).normalized;
```

---

## 4.3 CraneController
Одговорен за:
- movement input,
- ограничување на движењето,
- platform upgrades.

---

## 4.4 Block
Секој block:
- има HP,
- визуелен state,
- може да содржи скриен powerup.

---

## 4.5 PowerupManager
Главна класа за powerup системот.

Функционалности:
- случајно доделување powerups,
- spawn на powerup drops,
- activation на bonuses,
- audio playback.

---

## 4.6 UIManager
Автоматско:
- креирање UI,
- ажурирање текстови,
- прикажување end screens.

---

## 4.7 MainMenuManager
Контролира:
- старт на игра,
- quit application,
- scene loading.

---

# 5. Gameplay Flow

## Старт на игра
1. Играчот притиска `Space`.
2. Топката се лансира.
3. Тајмерот започнува.

## Во тек на игра
- Играчот ги уништува блоковите.
- Секој уништен блок додава +2 секунди.
- Powerup-ите можат да се соберат со платформата.

## Победа
- Кога нема преостанати блокови:
  - играчот победува,
  - се зголемува level counter,
  - следното ниво е потешко.

## Пораз
Играта завршува ако:
- истече времето,
- топката падне под платформата.

---

# 6. Main Menu

Имплементирано е:
- Start Game button
- Rules panel
- Quit button
- Background music
- ESC враќање од gameplay scene назад во менито

---

# 7. Технологии

Проектот користи:
- Unity Engine
- C#
- Unity Physics2D
- Unity UI System
- Unity Audio System
- SpriteRenderer
- Rigidbody2D
- Collider2D
- LineRenderer

---

# 8. Screenshots

## Main Gameplay

<img width="1919" height="1080" alt="meni" src="https://github.com/user-attachments/assets/8328f93a-d005-4b12-af1a-e4c5181c204b" /><br/>



<img width="1919" height="1074" alt="Igra" src="https://github.com/user-attachments/assets/07a72ed8-c5b0-4046-849c-21a2834af234" />


---

# 9. Користење на AI

Во текот на развојот беше користен ChatGPT за:
- debugging,
- рефакторирање,
- AI генерација на слики,
- имплементација на gameplay features,
- документација,
- UI и audio подобрувања.

Примери:
- додавање powerup систем,
- додавање menu navigation,
- progressive difficulty,
- audio manager логика,
- оптимизација на collision систем.

---

# 10. Заклучок

Проектот претставува комплетна 2D аркадна игра со:
- gameplay loop,
- progression system,
- powerups,
- UI,
- audio,
- menu system,
- scalable architecture.

Кодот е организиран во независни класи што овозможува:
- лесно одржување,
- додавање нови функционалности,
- лесно проширување на проектот.

Можни идни подобрувања:
- save/load систем,
- score system,
- нови powerups,
- particle effects,
- animations.
