#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
#if UNITY_2023_1_OR_NEWER
using UnityEditor.U2D.Sprites;
#endif

[ExecuteInEditMode]
public class SPUM_SpriteEditManager : MonoBehaviour
{
    public SPUM_SpriteList _spriteObj;
    public List<SpriteRenderer> _syncList = new List<SpriteRenderer>();

    public void SyncPivot()
    {
        SyncPivotProcess(_spriteObj._hairList[0]);
        SyncPivotProcess(_spriteObj._hairList[3]);
        SyncPivotProcess(_spriteObj._hairList[1]);
        SyncPivotProcess(_spriteObj._hairList[2]);
        SyncPivotProcess(_spriteObj._clothList[0]);
        SyncPivotProcess(_spriteObj._clothList[1]);
        SyncPivotProcess(_spriteObj._clothList[2]);
        SyncPivotProcess(_spriteObj._pantList[0]);
        SyncPivotProcess(_spriteObj._pantList[1]);
        SyncPivotProcess(_spriteObj._armorList[0]);
        SyncPivotProcess(_spriteObj._armorList[1]);
        SyncPivotProcess(_spriteObj._armorList[2]);
        SyncPivotProcess(_spriteObj._weaponList[0]);
        SyncPivotProcess(_spriteObj._weaponList[1]);
        SyncPivotProcess(_spriteObj._weaponList[2]);
        SyncPivotProcess(_spriteObj._weaponList[3]);
        SyncPivotProcess(_spriteObj._backList[0]);
    }

    public void SyncPivotProcess(SpriteRenderer SR)
    {
        if (SR.sprite != null)
        {
#if UNITY_2023_2_OR_NEWER
            SetPivot(SR, false);
#else
            SetPivot(SR);
#endif
        }
    }

    public void ResetPivot(SpriteRenderer SR)
    {
#if UNITY_2023_1_OR_NEWER
        SetPivot(SR, true);
#else
        if (SR.sprite != null)
        {
            string path = AssetDatabase.GetAssetPath(SR.sprite.texture);
            TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(path);
            Vector2 centerPivot = new Vector2(0.5f, 0.5f);
            ti.spritePivot = centerPivot;
            TextureImporterSettings texSettings = new TextureImporterSettings();
            ti.ReadTextureSettings(texSettings);
            texSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            ti.SetTextureSettings(texSettings);
            ti.SaveAndReimport();
            SR.transform.localPosition = Vector3.zero;
        }
#endif
    }

    public void SetMultiple(Texture2D sp, Sprite ttSP, string nameCode)
    {
        if (sp == null) return;
        string spritePath = AssetDatabase.GetAssetPath(sp);
        Object[] sprites = AssetDatabase.LoadAllAssetsAtPath(spritePath);
        foreach (object obj in sprites)
        {
            if (obj.GetType() == typeof(Sprite))
            {
                Sprite tSP = (Sprite)obj;
                if (tSP.name == nameCode) ttSP = tSP;
            }
        }
    }

    public void SetWeapon(Sprite sp, int Type)
    {
        if (sp != null)
        {
            string tRWName = sp.name;
            if (Type == 0)
            {
                if (tRWName.Contains("Shield"))
                {
                    _syncList[12].GetComponent<SpriteSync>()._nowSprite = null;
                    _syncList[13].GetComponent<SpriteSync>()._nowSprite = sp;
                }
                else
                {
                    _syncList[12].GetComponent<SpriteSync>()._nowSprite = sp;
                    _syncList[13].GetComponent<SpriteSync>()._nowSprite = null;
                }
            }
            else
            {
                if (tRWName.Contains("Shield"))
                {
                    _syncList[14].GetComponent<SpriteSync>()._nowSprite = null;
                    _syncList[15].GetComponent<SpriteSync>()._nowSprite = sp;
                }
                else
                {
                    _syncList[14].GetComponent<SpriteSync>()._nowSprite = sp;
                    _syncList[15].GetComponent<SpriteSync>()._nowSprite = null;
                }
            }
        }
    }

    public void EmptyAllSprite()
    {
        for (var i = 0; i < _syncList.Count; i++)
        {
            _syncList[i].sprite = null;
            _syncList[i].GetComponent<SpriteSync>()._nowSprite = null;
        }
    }

#pragma warning disable CS0618
    public void SetPivot(SpriteRenderer _sprite)
    {
        if (_sprite.transform.localPosition.x == 0 && _sprite.transform.localPosition.y == 0) return;
        string path = AssetDatabase.GetAssetPath(_sprite.sprite.texture);
        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(path);
        if (ti.spritesheet.Length > 1)
        {
            ti.isReadable = true;
            List<SpriteMetaData> newData = new List<SpriteMetaData>();
            for (var i = 0; i < ti.spritesheet.Length; i++)
            {
                SpriteMetaData tD = ti.spritesheet[i];
                tD.alignment = (int)SpriteAlignment.Custom;
                if (_sprite.sprite.name == tD.name)
                {
                    float tXSize = tD.rect.size.x;
                    float tYSize = tD.rect.size.y;
                    float tX = _sprite.transform.localPosition.x / 0.015625f;
                    float tY = _sprite.transform.localPosition.y / 0.015625f;
                    float ttX = tX / tXSize * 0.5f;
                    float ttY = tY / tYSize * 0.5f;
                    float rX = 0.5f - ttX;
                    float rY = 0.5f - ttY;
                    tD.pivot = new Vector2(rX, rY);
                    ti.spritesheet[i] = tD;
                }
                newData.Add(tD);
            }
            ti.spritesheet = newData.ToArray();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ti.isReadable = false;
        }
        else
        {
            float tXSize = _sprite.sprite.rect.size.x;
            float tYSize = _sprite.sprite.rect.size.y;
            float tX = _sprite.transform.localPosition.x / 0.015625f;
            float tY = _sprite.transform.localPosition.y / 0.015625f;
            float ttX = tX / tXSize * 0.5f;
            float ttY = tY / tYSize * 0.5f;
            float rX = 0.5f - ttX;
            float rY = 0.5f - ttY;
            Vector2 newPivot = new Vector2(rX, rY);
            ti.spritePivot = newPivot;
            TextureImporterSettings texSettings = new TextureImporterSettings();
            ti.ReadTextureSettings(texSettings);
            texSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            ti.SetTextureSettings(texSettings);
            ti.SaveAndReimport();
        }
        _sprite.transform.localPosition = new Vector3(0, 0, 0);
    }
#pragma warning restore CS0618

#if UNITY_2023_1_OR_NEWER
    public void SetPivot(SpriteRenderer _sprite, bool Reset = false)
    {
        string path = AssetDatabase.GetAssetPath(_sprite.sprite.texture);
        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ISpriteEditorDataProvider dataProvider = GetSpriteEditorDataProvider(ti);
        if (dataProvider == null) { Debug.LogError("Failed to get ISpriteEditorDataProvider"); return; }
        dataProvider.GetDataProvider<ITextureDataProvider>().GetTextureActualWidthAndHeight(out int actualWidth, out int actualHeight);
        SpriteRect[] spriteRects = dataProvider.GetSpriteRects();
        if (spriteRects.Length > 1)
        {
            ti.isReadable = true;
            for (var i = 0; i < spriteRects.Length; i++)
            {
                SpriteRect spriteRect = spriteRects[i];
                if (_sprite.sprite.name == spriteRect.name)
                    UpdateSpritePivot(spriteRect, _sprite, Reset);
            }
            dataProvider.SetSpriteRects(spriteRects);
            dataProvider.Apply();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            ti.isReadable = false;
        }
        else
        {
            SpriteRect spriteRect = spriteRects[0];
            UpdateSpritePivot(spriteRect, _sprite, Reset);
            dataProvider.SetSpriteRects(new[] { spriteRect });
            dataProvider.Apply();
            ti.SaveAndReimport();
        }
        _sprite.transform.localPosition = Vector3.zero;
    }

    private void UpdateSpritePivot(SpriteRect spriteRect, SpriteRenderer _sprite, bool Reset = false)
    {
        Vector2 localPosition = _sprite.transform.localPosition;
        Vector2 spriteSize = _sprite.sprite.rect.size;
        Vector2 currentPivot = spriteRect.pivot;
        Vector2 currentPivotInPixels = new Vector2(currentPivot.x * spriteSize.x, currentPivot.y * spriteSize.y);
        Vector2 newPivotInPixels = currentPivotInPixels - localPosition * (1f / 0.015625f * .5f);
        Vector2 normalizedNewPivot = new Vector2(newPivotInPixels.x / spriteSize.x, newPivotInPixels.y / spriteSize.y);
        normalizedNewPivot.x = Mathf.Clamp01(normalizedNewPivot.x);
        normalizedNewPivot.y = Mathf.Clamp01(normalizedNewPivot.y);
        spriteRect.alignment = SpriteAlignment.Custom;
        spriteRect.pivot = Reset ? Vector2.one * .5f : normalizedNewPivot;
    }

    private ISpriteEditorDataProvider GetSpriteEditorDataProvider(TextureImporter importer)
    {
        var dataProviderFactories = new SpriteDataProviderFactories();
        dataProviderFactories.Init();
        var dataProvider = dataProviderFactories.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();
        return dataProvider;
    }
#endif
}
#endif