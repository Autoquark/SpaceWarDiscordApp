using System.Collections;
using System.Collections.ObjectModel;
using Google.Cloud.Firestore;

namespace SpaceWarDiscordApp.Database;

public class LinkedDocumentCollection
{
    
}

public class LinkedDocumentCollection<T> : IEnumerable<T> where T : FirestoreModel
{
    private readonly Func<IList<DocumentReference>> _documentList;

    public LinkedDocumentCollection(CollectionReference trackedCollection, Func<IList<DocumentReference>> documentList)
    {
        _documentList = documentList;
        TrackedCollection = trackedCollection;
        Items = new ReadOnlyCollection<T>(_items);
    }

    public CollectionReference TrackedCollection { get; }

    public IReadOnlyList<T> Items { get; }
    
    private readonly List<T> _items = [];

    private readonly List<DocumentReference> _removed = [];

    public void Add(T item)
    {
        item.DocumentId = TrackedCollection.Document();
        _items.Add(item);
    }

    public bool Remove(T item)
    {
        if (!_items.Remove(item) || item.DocumentId == null)
        {
            return false;
        }
        
        _removed.Add(item.DocumentId);
        return true;
    }

    public void RemoveAt(int index)
    {
        _removed.Add(_items[index].DocumentId!);
        _items.RemoveAt(index);
    }

    public async Task PopulateAsync(Transaction transaction)
    {
        var snapshots = (await transaction.GetAllSnapshotsAsync(_documentList()))
            .WhereNonNull();

        _items.Clear();
        _items.AddRange(typeof(T).IsAssignableTo(typeof(PolymorphicFirestoreModel))
            ? snapshots.Select(x => (T)(object)x.ConvertToPolymorphic<PolymorphicFirestoreModel>())
            : snapshots.Select(x => x.ConvertTo<T>()));
    }

    public void OnSavingParentDoc(Transaction transaction)
    {
        foreach (var documentReference in _removed)
        {
            transaction.Delete(documentReference);
            // Can't remove the document reference from _removed, because this transaction might need to be rerun
            // Fortunately this LinkedDocumentCollection isn't likely to be around very long as we're constantly
            // refetching the game from the database
        }

        var list = _documentList();
        list.Clear();
        
        // Do an add or overwrite operation for every item
        foreach (var firestoreModel in _items)
        {
            firestoreModel.DocumentId ??= TrackedCollection.Document();
            transaction.Set(firestoreModel.DocumentId, firestoreModel);
            
            // Also store the document id in our list of linked documents for saving to the DB
            list.Add(firestoreModel.DocumentId);
        }
    }
    
    public IEnumerator<T> GetEnumerator() => Items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}