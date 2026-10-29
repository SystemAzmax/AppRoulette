using AppRoulette.Models;
using AppRoulette.Services;

namespace AppRoulette.Tests.Fakes;

/// <summary>
/// テスト用の <see cref="IGroupRepository"/> フェイク実装です。
/// </summary>
internal class FakeGroupRepository : IGroupRepository
{
    private readonly List<GroupRecord> _groups = new();
    private int _nextId = 1;

    /// <summary>
    /// グループ一覧の取得で例外を投げるかどうかを取得または設定します。
    /// </summary>
    public bool ShouldFailOnLoad { get; set; }

    /// <summary>
    /// グループの追加で例外を投げるかどうかを取得または設定します。
    /// </summary>
    public bool ShouldFailOnAdd { get; set; }

    /// <summary>
    /// 有効なグループ一覧を表示順で非同期に取得します。
    /// </summary>
    /// <returns>有効なグループ一覧。</returns>
    public Task<IReadOnlyList<RouletteGroup>> GetGroupsAsync()
    {
        if (ShouldFailOnLoad)
        {
            throw new IOException("テスト用の読み込み失敗");
        }

        var groups = _groups
            .Where(g => !g.IsDeleted)
            .OrderBy(g => g.SortOrder)
            .ThenBy(g => g.Id)
            .Select(g => new RouletteGroup(g.Id, g.DisplayName)
            {
                ExcludeOnWin = g.ExcludeOnWin
            })
            .ToList();

        return Task.FromResult((IReadOnlyList<RouletteGroup>)groups);
    }

    /// <summary>
    /// 既定グループが未登録の場合に補完します。
    /// </summary>
    /// <param name="groups">補完対象の既定グループ一覧。</param>
    public Task EnsureDefaultGroupsAsync(IReadOnlyList<RouletteGroup> groups)
    {
        foreach (var group in groups)
        {
            if (_groups.All(g => g.Id != group.Id))
            {
                _groups.Add(new GroupRecord(
                    group.Id,
                    group.DisplayName,
                    group.Id,
                    isDeleted: false));
                _nextId = Math.Max(_nextId, group.Id + 1);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 保存済みのグループ名を非同期で取得します。
    /// </summary>
    /// <returns>グループIDと表示名の対応表。</returns>
    public Task<IReadOnlyDictionary<int, string>> GetGroupNamesAsync() =>
        Task.FromResult((IReadOnlyDictionary<int, string>)_groups
            .Where(g => !g.IsDeleted)
            .ToDictionary(g => g.Id, g => g.DisplayName));

    /// <summary>
    /// 指定されたグループ名を非同期で保存します。
    /// </summary>
    /// <param name="groupId">保存対象グループの識別子。</param>
    /// <param name="displayName">保存する表示名。</param>
    public Task SaveGroupNameAsync(int groupId, string displayName)
    {
        var group = _groups.FirstOrDefault(g => g.Id == groupId);
        if (group is not null)
        {
            group.DisplayName = displayName;
        }
        else
        {
            _groups.Add(new GroupRecord(groupId, displayName, groupId, isDeleted: false));
            _nextId = Math.Max(_nextId, groupId + 1);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 指定されたグループの「当たったら除外」設定を非同期で保存します。
    /// </summary>
    /// <param name="groupId">保存対象グループの識別子。</param>
    /// <param name="excludeOnWin">「当たったら除外」の有効状態。</param>
    public Task SaveGroupExcludeOnWinAsync(int groupId, bool excludeOnWin)
    {
        var group = _groups.FirstOrDefault(g => g.Id == groupId);
        if (group is not null)
        {
            group.ExcludeOnWin = excludeOnWin;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 新しいグループを非同期で追加します。
    /// </summary>
    /// <param name="displayName">追加するグループの表示名。</param>
    /// <param name="sortOrder">追加するグループの表示順。</param>
    /// <returns>追加されたグループ。</returns>
    public Task<RouletteGroup> AddGroupAsync(string displayName, int sortOrder)
    {
        if (ShouldFailOnAdd)
        {
            throw new IOException("テスト用の保存失敗");
        }

        var group = new GroupRecord(_nextId++, displayName, sortOrder, isDeleted: false);
        _groups.Add(group);
        return Task.FromResult(new RouletteGroup(group.Id, group.DisplayName));
    }

    /// <summary>
    /// 指定されたグループを非同期で削除済みにします。
    /// </summary>
    /// <param name="groupId">削除対象グループの識別子。</param>
    public Task DeleteGroupAsync(int groupId)
    {
        var group = _groups.FirstOrDefault(g => g.Id == groupId);
        if (group is not null)
        {
            group.IsDeleted = true;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// グループ一覧の表示順を非同期で保存します。
    /// </summary>
    /// <param name="groups">表示順を保存するグループ一覧。</param>
    public Task SaveGroupOrderAsync(IReadOnlyList<RouletteGroup> groups)
    {
        for (var i = 0; i < groups.Count; i++)
        {
            var group = _groups.First(g => g.Id == groups[i].Id);
            group.SortOrder = i + 1;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 全グループを削除済みにしたうえで、指定したグループ一覧で置き換えます。
    /// <see cref="FakeBackupRepository"/> から呼び出します。
    /// </summary>
    /// <param name="groups">置き換え後のグループ一覧（表示順）。</param>
    public void ReplaceAll(IReadOnlyList<RouletteGroup> groups)
    {
        foreach (var record in _groups)
        {
            record.IsDeleted = true;
        }

        for (var i = 0; i < groups.Count; i++)
        {
            var group = groups[i];
            var record = _groups.FirstOrDefault(g => g.Id == group.Id);
            if (record is null)
            {
                record = new GroupRecord(group.Id, group.DisplayName, i + 1, isDeleted: false);
                _groups.Add(record);
            }

            record.DisplayName = group.DisplayName;
            record.SortOrder = i + 1;
            record.IsDeleted = false;
            record.ExcludeOnWin = group.ExcludeOnWin;
            _nextId = Math.Max(_nextId, group.Id + 1);
        }
    }

    private sealed class GroupRecord
    {
        public GroupRecord(
            int id,
            string displayName,
            int sortOrder,
            bool isDeleted)
        {
            Id = id;
            DisplayName = displayName;
            SortOrder = sortOrder;
            IsDeleted = isDeleted;
        }

        public int Id { get; }

        public string DisplayName { get; set; }

        public int SortOrder { get; set; }

        public bool IsDeleted { get; set; }

        public bool ExcludeOnWin { get; set; }
    }
}
