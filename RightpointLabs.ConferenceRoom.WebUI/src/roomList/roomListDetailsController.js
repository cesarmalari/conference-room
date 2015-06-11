(function() {
    'use strict;'

    angular.module('app').controller('RoomListDetailsController', ['Restangular', '$stateParams', 'timelineService', 'localStorageService', function(Restangular, $stateParams, timelineService, localStorageService) {
        var self = this;
        self.roomLists = [];
        self.moment = window.moment;

        var defaultRoom = localStorageService.get('defaultRoom');
        Restangular.one('roomList', $stateParams.roomListAddress).getList('rooms').then(function(data) {
            angular.forEach(data, function(room) {
                room.sortOrder = parseInt(room.Name.replace(/[^0-9]/g, '') || '99999');
            });
            data.sort(function(a,b) {
                if(a.sortOrder == b.sortOrder) {
                    return a.Name < b.Name ? -1 : 1;
                }
                return a.sortOrder < b.sortOrder ? -1 : 1;
            });

            self.rooms = data;
            angular.forEach(self.rooms, function(room) {
                room.isLoading = true;
                room.isDefault = room.Address == defaultRoom;
                return Restangular.one('room', room.Address).one('status').get().then(function(data) {
                    room.isLoading = false;
                    if(!data || data.error) {
                        room.status = {};
                        room.appointments = [];
                        room.current = null;
                        room.next = null;
                        return;
                    }

                    room.status = data.Status;
                    room.appointments = _.sortBy(data.NearTermMeetings, 'Start');
                    room.current = data.CurrentMeeting;
                    room.next = data.NextMeeting;

                    var now = self.moment();
                    var start = now.clone();
                    var end = now.clone().add(1, 'hours');

                    room.timeline = timelineService.build(start, end, room.appointments);
                }, function() {
                    room.isLoading = false;
                    room.status = null;
                });
            });
        });
    }]);
})();