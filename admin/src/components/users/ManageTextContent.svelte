<script lang="ts">
    import Permission from '../Permission.svelte';
    import moment from 'dayjs';
    import request from '../../lib/request';
    import Confirm from '../modal/Confirm.svelte';

    export let userId: string|number;
    let textView: string = 'Status';

    let modalBody: string|undefined;
    let modalCb: (didClickYes: boolean) => void|undefined;
    let modalVisible: boolean = false;
    let endpoint: string = `/user/status-history?userId=${userId}`;
    let errorMessage: string|undefined;

    $: {
        switch (textView) {
            case 'Status':
                endpoint = `/user/status-history?userId=${userId}`;
                break;
            case 'Comments':
                endpoint = `/user/comment-history?userId=${userId}`;
                break;
            case 'Invites':
                endpoint = `/invites/${userId}`;
                break;
            case 'Bans':
                endpoint = `/user/ban-history?userId=${userId}`;
                break;
            default:
                endpoint = `/user/status-history?userId=${userId}`;
        }
    }
</script>

<div class="row">
    <div class="col-12 mt-4">
        {#if modalVisible}
            <Confirm
                title="Confirm"
                message={modalBody}
                cb={(e) => {
                    modalVisible = false;
                    modalCb?.(e);
                }}
            />
        {/if}

        {#if errorMessage}
            <p class="text-danger mt-4 mb-4">{errorMessage}</p>
        {/if}

        <div class="btn-group">
            <Permission p="GetUserStatusHistory">
                <button
                    class={textView === "Status" ? "btn btn-primary" : "btn btn-outline-primary"}
                    on:click={(e) => { e.preventDefault(); textView = 'Status'; }}
                >Status</button>
            </Permission>

            <Permission p="GetUserCommentHistory">
                <button
                    class={textView === "Comments" ? "btn btn-primary" : "btn btn-outline-primary"}
                    on:click={(e) => { e.preventDefault(); textView = 'Comments'; }}
                >Comments</button>
            </Permission>

            <Permission p="ManageInvites">
                <button
                    class={textView === "Invites" ? "btn btn-primary" : "btn btn-outline-primary"}
                    on:click={(e) => { e.preventDefault(); textView = 'Invites'; }}
                >Invites</button>
            </Permission>

            <Permission p="BanUser">
                <button
                    class={textView === "Bans" ? "btn btn-primary" : "btn btn-outline-primary"}
                    on:click={(e) => { e.preventDefault(); textView = 'Bans'; }}
                >Ban History</button>
            </Permission>
        </div>
    </div>

    {#await request.get(endpoint) then data}
        {#each data.data as item}
            <div class="col-12">
                <div class="card card-body mt-2 bg-dark text-white">
                    {#if textView === 'Invites'}
                        <p class="mb-0">
                            <a href={`/admin/manage-user/${item.userId}`}>Invited {item.userId}</a>
                        </p>
                    {/if}


                    {#if textView === 'Bans'}
                        <p class="mb-0">
                            <span class="fw-bold">Ban ID:</span>
                            <span class="monospace">{item.id}</span>
                        </p>
                        <p class="mb-0">
                            <span class="fw-bold">Author:</span>
                            <a href={`/admin/manage-user/${item.actor_id}`}>{item.actor_username}</a>
                        </p>
                        <p class="mb-0">
                            <span class="fw-bold">Reason:</span>
                            <span class="monospace">{item.reason}</span>
                        </p>
                        <p class="mb-0">
                            <span class="fw-bold">Internal Reason:</span>
                            <span class="monospace">{item.internal_reason}</span>
                        </p>
                        <p class="mb-0">
                            <span class="fw-bold">Created:</span>
                            {item.created_at ? moment(item.created_at).format("MMM DD YYYY, h:mm A") : 'Never'}
                        </p>
                        <p class="mb-0">
                            <span class="fw-bold">Expires:</span>
                            {item.expired_at ? moment(item.expired_at).format("MMM DD YYYY, h:mm A") : 'Never'}
                        </p>
                    {:else}
                        <p class="mb-0">{item.status || item.comment}</p>
                        <p class="mb-0">
                            {moment(item.created_at || item.createdAt).format("MMM DD YYYY, h:mm A")}
                        </p>
                    {/if}

                    {#if textView === 'Status' || textView === 'Comments'}
                        <div class="row">
                            <div class="col-6 col-lg-3">
                                <button
                                    class="btn btn-outline-danger btn-sm mt-2"
                                    on:click={(e) => {
                                        e.preventDefault();
                                        const deleteUrl = textView === 'Status'
                                            ? `/user/status?userId=${userId}&statusId=${item.id}`
                                            : `/user/comment?userId=${userId}&commentId=${item.id}`;

                                        modalBody = `Confirm that you want to delete this content: ${item.status || item.comment}`;
                                        modalCb = (confirmed) => {
                                            if (confirmed) {
                                                request.delete(deleteUrl)
                                                    .then(() => location.reload())
                                                    .catch(err => { errorMessage = err.message; });
                                            }
                                        };
                                        modalVisible = true;
                                    }}
                                >Delete</button>
                            </div>
                        </div>
                    {/if}
                </div>
            </div>
        {/each}
    {/await}
</div>